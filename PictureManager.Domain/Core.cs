using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain {
  public sealed class Core : ILogger {
    public string CachePath { get; set; }
    public int ThumbnailSize { get; set; }
    public double WindowsDisplayScale { get; set; }
    public ObservableCollection<LogItem> Log { get; set; } = new();
    public ILogger Logger { get; set; }

    #region DB Models
    public CategoryGroupsM CategoryGroupsM { get; }
    public FavoriteFoldersM FavoriteFoldersM { get; }
    public FoldersM FoldersM { get; }
    public PeopleM PeopleM { get; }
    public FolderKeywordsM FolderKeywordsM { get; }
    public KeywordsM KeywordsM { get; }
    public GeoNamesM GeoNamesM { get; }
    public ViewersM ViewersM { get; }
    public MediaItemsM MediaItemsM { get; }
    public VideoClipsM VideoClipsM { get; }
    public VideoClipsGroupsM VideoClipsGroupsM { get; }
    #endregion

    public SimpleDB.SimpleDB Sdb { get; private set; }
    public Segments Segments { get; }
    public ViewerM CurrentViewer { get; set; }
    public ThumbnailsGridsM ThumbnailsGridsM { get; }

    private TaskScheduler UiTaskScheduler { get; }

    private Core() {
      UiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

      Sdb = new(this);

      FavoriteFoldersM = new(this);
      FoldersM = new(this);
      PeopleM = new(this);
      FolderKeywordsM = new(this);
      KeywordsM = new(this);
      GeoNamesM = new(this);
      ViewersM = new(this);

      CategoryGroupsM = new(this);
      CategoryGroupsM.Categories.Add(Category.People, PeopleM);
      CategoryGroupsM.Categories.Add(Category.Keywords, KeywordsM);

      MediaItemsM = new(this);
      VideoClipsM = new(this);
      VideoClipsGroupsM = new(this);
      Segments = new(this);
      ThumbnailsGridsM = new(this);
    }

    public Task InitAsync(IProgress<string> progress) {
      return Task.Run(() => {
        Sdb.AddDataAdapter(CategoryGroupsM.DataAdapter); // needs to be before People and Keywords
        Sdb.AddDataAdapter(KeywordsM.DataAdapter);
        Sdb.AddDataAdapter(FoldersM.DataAdapter); // needs to be before Viewers
        Sdb.AddDataAdapter(ViewersM.DataAdapter);
        Sdb.AddDataAdapter(PeopleM.DataAdapter); // needs to be before Segments
        Sdb.AddDataAdapter(GeoNamesM.DataAdapter);
        Sdb.AddDataAdapter(MediaItemsM.DataAdapter);
        Sdb.AddDataAdapter(VideoClipsGroupsM.DataAdapter); // needs to be before VideoClips
        Sdb.AddDataAdapter(VideoClipsM.DataAdapter);
        Sdb.AddDataAdapter(FavoriteFoldersM.DataAdapter);
        Sdb.AddDataAdapter(Segments.DataAdapter);

        Sdb.LoadAllTables(progress);
        Sdb.LinkReferences(progress);

        progress.Report("Loading Drives");
        FoldersM.AddDrives();
        progress.Report("Loading Folder Keywords");
        FolderKeywordsM.Load();

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
        Segments.AllDic.Clear();
        Segments.AllDic = null;
      });
    }

    public bool CanViewerSeeThisFolder(FolderM folder) => CurrentViewer?.CanSeeThisFolder(folder) != false;

    public bool CanViewerSeeContentOfThisFolder(FolderM folder) => CurrentViewer?.CanSeeContentOfThisFolder(folder) != false;

    public bool CanViewerSee(MediaItemM mediaItem) => CurrentViewer?.CanSee(mediaItem) != false;

    public void LogError(Exception ex) => LogError(ex, string.Empty);

    public void LogError(Exception ex, string msg) =>
      RunOnUiThread(() =>
        Log.Add(new LogItem(string.IsNullOrEmpty(msg) ? ex.Message : msg, $"{msg}\n{ex.Message}\n{ex.StackTrace}")));

    private static Core _instance;
    private static readonly object Lock = new();
    public static Core Instance {
      get {
        lock (Lock) {
          return _instance ??= new();
        }
      }
    }

    public Task RunOnUiThread(Action action) {
      var task = new Task(action);
      task.Start(UiTaskScheduler);
      return task;
    }

    public Task<T> RunOnUiThread<T>(Func<T> func) {
      var task = new Task<T>(func);
      task.Start(UiTaskScheduler);
      return task;
    }
  }
}
