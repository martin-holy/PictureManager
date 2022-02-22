using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain {
  public sealed class Core : ILogger {
    public string CachePath { get; set; }
    public int ThumbnailSize { get; set; }
    public ObservableCollection<LogItem> Log { get; } = new();
    public SimpleDB.SimpleDB Sdb { get; }
    public TitleProgressBarM TitleProgressBarM { get; } = new();

    public CategoryGroupsM CategoryGroupsM { get; }
    public FavoriteFoldersM FavoriteFoldersM { get; }
    public FolderKeywordsM FolderKeywordsM { get; }
    public FoldersM FoldersM { get; }
    public GeoNamesM GeoNamesM { get; }
    public KeywordsM KeywordsM { get; }
    public MediaItemsM MediaItemsM { get; }
    public PeopleM PeopleM { get; }
    public SegmentsM SegmentsM { get; }
    public ThumbnailsGridsM ThumbnailsGridsM { get; }
    public VideoClipsM VideoClipsM { get; }
    public ViewersM ViewersM { get; }

    private static TaskScheduler UiTaskScheduler { get; set; }

    private Core() {
      UiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

      Sdb = new(this);

      ViewersM = new();
      SegmentsM = new();
      CategoryGroupsM = new();
      FavoriteFoldersM = new();
      FolderKeywordsM = new();
      FoldersM = new(this, ViewersM); // FolderKeywordsM, MediaItemsM
      GeoNamesM = new();
      KeywordsM = new();
      MediaItemsM = new(this, SegmentsM, ViewersM); // ThumbnailsGridsM
      PeopleM = new();
      ThumbnailsGridsM = new();
      VideoClipsM = new();

      CategoryGroupsM.Categories.Add(Category.People, PeopleM);
      CategoryGroupsM.Categories.Add(Category.Keywords, KeywordsM);

      CategoryGroupsM.DataAdapter = new CategoryGroupsDataAdapter(Sdb, CategoryGroupsM, KeywordsM, PeopleM);
      FavoriteFoldersM.DataAdapter = new FavoriteFoldersDataAdapter(Sdb, FavoriteFoldersM, FoldersM);
      FoldersM.DataAdapter = new FoldersDataAdapter(Sdb, FoldersM);
      GeoNamesM.DataAdapter = new GeoNamesDataAdapter(Sdb, GeoNamesM);
      KeywordsM.DataAdapter = new KeywordsDataAdapter(Sdb, KeywordsM, CategoryGroupsM);
      MediaItemsM.DataAdapter = new MediaItemsDataAdapter(Sdb, MediaItemsM, FoldersM, PeopleM, KeywordsM, GeoNamesM);
      PeopleM.DataAdapter = new PeopleDataAdapter(Sdb, PeopleM, SegmentsM, KeywordsM);
      SegmentsM.DataAdapter = new SegmentsDataAdapter(Sdb, SegmentsM, MediaItemsM, PeopleM, KeywordsM);
      VideoClipsM.DataAdapter = new VideoClipsDataAdapter(Sdb, VideoClipsM, MediaItemsM, KeywordsM, PeopleM);
      VideoClipsM.GroupsM.DataAdapter = new VideoClipsGroupsDataAdapter(Sdb, VideoClipsM.GroupsM, VideoClipsM, MediaItemsM);
      ViewersM.DataAdapter = new ViewersDataAdapter(Sdb, ViewersM, FoldersM, KeywordsM);
    }

    public Task InitAsync(IProgress<string> progress) {
      return Task.Run(() => {
        Sdb.AddDataAdapter(CategoryGroupsM.DataAdapter); // needs to be before People and Keywords
        Sdb.AddDataAdapter(KeywordsM.DataAdapter);
        Sdb.AddDataAdapter(FoldersM.DataAdapter); // needs to be before Viewers and FavoriteFolders
        Sdb.AddDataAdapter(ViewersM.DataAdapter);
        Sdb.AddDataAdapter(PeopleM.DataAdapter); // needs to be before Segments
        Sdb.AddDataAdapter(GeoNamesM.DataAdapter);
        Sdb.AddDataAdapter(MediaItemsM.DataAdapter);
        Sdb.AddDataAdapter(VideoClipsM.GroupsM.DataAdapter); // needs to be before VideoClips
        Sdb.AddDataAdapter(VideoClipsM.DataAdapter);
        Sdb.AddDataAdapter(FavoriteFoldersM.DataAdapter);
        Sdb.AddDataAdapter(SegmentsM.DataAdapter);

        Sdb.LoadAllTables(progress);
        Sdb.LinkReferences(progress);

        progress.Report("Loading Drives");
        FoldersM.AddDrives();
        progress.Report("Loading Folder Keywords");
        FolderKeywordsM.Load(FoldersM.All);

        AttachEvents();

        // TODO better
        // cleanup
        FoldersM.AllDic.Clear();
        FoldersM.AllDic = null;
        GeoNamesM.AllDic.Clear();
        GeoNamesM.AllDic = null;
        KeywordsM.AllDic.Clear();
        KeywordsM.AllDic = null;
        MediaItemsM.AllDic.Clear();
        MediaItemsM.AllDic = null;
        PeopleM.AllDic.Clear();
        PeopleM.AllDic = null;
        VideoClipsM.AllDic.Clear();
        VideoClipsM.AllDic = null;
        SegmentsM.AllDic.Clear();
        SegmentsM.AllDic = null;
      });
    }

    private void AttachEvents() {
      FoldersM.FolderDeletedEvent += (_, e) => {
        FavoriteFoldersM.ItemDelete(e.Folder);
        MediaItemsM.Delete(e.Folder.MediaItems.ToList());
      };

      PeopleM.PersonDeletedEvent += (_, e) => {
        MediaItemsM.RemovePersonFromMediaItems(e.Person);
        SegmentsM.RemovePersonFromSegments(e.Person);
      };

      KeywordsM.KeywordDeletedEvent += (_, e) => {
        PeopleM.RemoveKeywordFromPeople(e.Keyword);
        SegmentsM.RemoveKeywordFromSegments(e.Keyword);
        MediaItemsM.RemoveKeywordFromMediaItems(e.Keyword);
      };

      MediaItemsM.MediaItemDeletedEvent += (_, e) => {
        SegmentsM.Delete(e.MediaItem.Segments);
        ThumbnailsGridsM.RemoveMediaItem(e.MediaItem);
      };

      CategoryGroupsM.CategoryGroupDeletedEvent += (_, e) => {
        // move all group items to root
        foreach (var item in e.Group.Items.ToArray()) {
          switch (e.Group.Category) {
            case Category.Keywords:
              KeywordsM.ItemMove(item, KeywordsM, false);
              break;
            case Category.People:
              PeopleM.ItemMove(item, PeopleM, false);
              break;
          }
        }
      };

      SegmentsM.SegmentPersonChangeEvent += (_, e) => {
        PeopleM.SegmentPersonChange(e.Segment, e.OldPerson, e.NewPerson);
        e.Segment.MediaItem.SetInfoBox();
      };

      ViewersM.PropertyChanged += (_, e) => {
        if (nameof(ViewersM.Current).Equals(e.PropertyName)) {
          FoldersM.AddDrives();
          FolderKeywordsM.Load(FoldersM.All);
          CategoryGroupsM.UpdateVisibility(ViewersM.Current);
        }
      };
    }

    public void LogError(Exception ex) =>
      LogError(ex, string.Empty);

    public void LogError(Exception ex, string msg) =>
      RunOnUiThread(() =>
        Log.Add(new(
          string.IsNullOrEmpty(msg)
            ? ex.Message
            : msg,
          $"{msg}\n{ex.Message}\n{ex.StackTrace}")));

    private static Core _instance;
    private static readonly object Lock = new();
    public static Core Instance {
      get {
        lock (Lock) {
          return _instance ??= new();
        }
      }
    }

    public static Task RunOnUiThread(Action action) {
      var task = new Task(action);
      task.Start(UiTaskScheduler);
      return task;
    }

    public static Task<T> RunOnUiThread<T>(Func<T> func) {
      var task = new Task<T>(func);
      task.Start(UiTaskScheduler);
      return task;
    }
  }
}
