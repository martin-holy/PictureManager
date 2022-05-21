using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MH.Utils.Dialogs;
using MH.Utils.Interfaces;
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
    public MediaItemSizesTreeM MediaItemSizesTreeM { get; }
    public PeopleM PeopleM { get; }
    public RatingsTreeM RatingsTreeM { get; }
    public SegmentsM SegmentsM { get; }
    public StatusPanelM StatusPanelM { get; }
    public ThumbnailsGridsM ThumbnailsGridsM { get; }
    public TreeViewCategoriesM TreeViewCategoriesM { get; }
    public VideoClipsM VideoClipsM { get; }
    public ViewersM ViewersM { get; }

    public static Func<IDialog, int> DialogHostShow { get; set; }

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
      KeywordsM = new(CategoryGroupsM);
      MediaItemsM = new(this, SegmentsM, ViewersM); // ThumbnailsGridsM
      MediaItemSizesTreeM = new();
      PeopleM = new(CategoryGroupsM);
      RatingsTreeM = new();
      StatusPanelM = new(this);
      ThumbnailsGridsM = new(this);
      TreeViewCategoriesM = new(this);
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
        SimpleDB.SimpleDB.Migrate(1, DatabaseMigration.Resolver, this);

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
        Sdb.ClearDataAdapters();

        AttachEvents();

        progress.Report("Loading drives");
        ViewersM.SetCurrent(ViewersM.Current);
      });
    }

    private void AttachEvents() {
      FoldersM.FolderDeletedEventHandler += (_, e) => {
        FavoriteFoldersM.ItemDelete(e.Data);
        MediaItemsM.Delete(e.Data.MediaItems.ToArray());
      };

      PeopleM.AfterItemRenameEventHandler += (_, e) => {
        MediaItemsM.UpdateInfoBoxWithPerson((PersonM)e.Data);
      };

      PeopleM.PersonDeletedEventHandler += (_, e) => {
        MediaItemsM.RemovePersonFromMediaItems(e.Data);
        SegmentsM.RemovePersonFromSegments(e.Data);
      };

      KeywordsM.AfterItemRenameEventHandler += (_, e) => {
        MediaItemsM.UpdateInfoBoxWithKeyword((KeywordM)e.Data);
      };

      KeywordsM.KeywordDeletedEventHandler += (_, e) => {
        PeopleM.RemoveKeywordFromPeople(e.Data);
        SegmentsM.RemoveKeywordFromSegments(e.Data);
        MediaItemsM.RemoveKeywordFromMediaItems(e.Data);
      };

      MediaItemsM.MediaItemDeletedEventHandler += (_, e) => {
        SegmentsM.Delete(e.Data.Segments);
        ThumbnailsGridsM.RemoveMediaItem(e.Data);
      };

      MediaItemsM.MetadataChangedEventHandler += (_, _) => {
        TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
        StatusPanelM.UpdateRating();
      };

      SegmentsM.SegmentPersonChangeEventHandler += (_, e) => {
        PeopleM.SegmentPersonChange(e.Data.Item1, e.Data.Item2, e.Data.Item3);
        e.Data.Item1.MediaItem.SetInfoBox();
      };

      ViewersM.PropertyChanged += (_, e) => {
        if (nameof(ViewersM.Current).Equals(e.PropertyName)) {
          FoldersM.AddDrives();
          FolderKeywordsM.Load(FoldersM.All);
          CategoryGroupsM.UpdateVisibility(ViewersM.Current);
        }
      };

      ThumbnailsGridsM.PropertyChanged += (_, e) => {
        if (nameof(ThumbnailsGridsM.Current).Equals(e.PropertyName)) {
          MediaItemSizesTreeM.Size.CurrentGrid = ThumbnailsGridsM.Current;
          TreeViewCategoriesM.MarkUsedKeywordsAndPeople();
        }
      };
    }

    private MessageDialog ToggleOrGetDialog(string title, object item, string itemName) {
      var sCount = SegmentsM.Selected.Count;
      var pCount = item is PersonM ? 0 : PeopleM.Selected.Count;
      var miCount = MediaItemsM.IsEditModeOn ? ThumbnailsGridsM.Current?.SelectedCount ?? 0 : 0;
      if (sCount == 0 && pCount == 0 && miCount == 0) return null;

      var msgA = $"Do you want to toggle #{itemName} on selected";
      var msgB = new List<string>();
      var msgS = sCount > 1 ? $"Segments ({sCount})" : "Segment";
      var msgP = pCount > 1 ? $"People ({pCount})" : "Person";
      var msgMi = miCount > 1 ? $"Media Items ({miCount})" : "Media Item";
      var oneOption = new[] { sCount, pCount, miCount }.Count(x => x > 0) == 1;
      var buttons = new List<DialogButton>();

      void AddOption(string msg, int result, string icon) {
        buttons.Add(oneOption
          ? new("YES", result, Res.IconCheckMark, true)
          : new(msg, result, icon));
        msgB.Add(msg);
      }

      if (sCount > 0) AddOption(msgS, 1, Res.IconEquals);
      if (pCount > 0) AddOption(msgP, 2, Res.IconPeople);
      if (miCount > 0) AddOption(msgMi, 3, Res.IconImage);
      if (oneOption) buttons.Add(new("NO", 0, Res.IconXCross, false, true));

      if (oneOption && miCount > 0) {
        MediaItemsM.SetMetadata(item);
        return null;
      }

      var msg = oneOption
        ? $"{msgA} {msgB[0]}?"
        : $"{msgA} {string.Join(" or ", msgB)}?";

      return new(title, msg, Res.IconQuestion, true, buttons.ToArray());
    }

    public void ToggleKeyword(KeywordM keyword) {
      if (ToggleOrGetDialog("Toggle Keyword", keyword, keyword.FullName) is not { } md) return;

      switch (DialogHostShow(md)) {
        case 1: SegmentsM.ToggleKeywordOnSelected(keyword); break;
        case 2: PeopleM.ToggleKeywordOnSelected(keyword); break;
        case 3: MediaItemsM.SetMetadata(keyword); break;
      }
    }

    public void TogglePerson(PersonM person) {
      if (ToggleOrGetDialog("Toggle Person", person, person.Name) is not { } md) return;

      switch (DialogHostShow(md)) {
        case 1: SegmentsM.SetSelectedAsPerson(person); break;
        case 3: MediaItemsM.SetMetadata(person); break;
      }
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
