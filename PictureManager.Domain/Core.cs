using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain {
  public sealed class Core : ILogger {
    public string CachePath { get; set; }
    public int ThumbnailSize { get; set; }
    public double WindowsDisplayScale { get; set; }
    public ObservableCollection<LogItem> Log { get; set; } = new();
    public ILogger Logger { get; set; }

    #region DB Models
    public FavoriteFoldersM FavoriteFoldersM { get; }
    public PeopleM PeopleM { get; }
    public CategoryGroupsM CategoryGroupsM { get; }
    public ViewersM ViewersM { get; }
    #endregion

    #region TreeView Roots and Categories
    public Folders Folders { get; }
    public Ratings Ratings { get; }
    public MediaItemSizes MediaItemSizes { get; }
    public FolderKeywords FolderKeywords { get; }
    public KeywordsM KeywordsM { get; }
    public GeoNames GeoNames { get; }
    #endregion

    public SimpleDB.SimpleDB Sdb { get; private set; }
    public MediaItems MediaItems { get; }
    public VideoClips VideoClips { get; }
    public VideoClipsGroups VideoClipsGroups { get; }
    public Segments Segments { get; }
    public ViewerM CurrentViewer { get; set; }
    public double ThumbScale { get; set; } = 1.0;

    private TaskScheduler UiTaskScheduler { get; }

    private Core() {
      UiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

      Sdb = new(this);

      FavoriteFoldersM = new(this);
      Folders = new(this);
      Ratings = new();
      MediaItemSizes = new();
      PeopleM = new(this);
      FolderKeywords = new();
      KeywordsM = new(this);
      GeoNames = new(this);
      ViewersM = new(this);

      
      CategoryGroupsM = new(this);
      CategoryGroupsM.Categories.Add(Category.People, PeopleM);
      CategoryGroupsM.Categories.Add(Category.Keywords, KeywordsM);

      MediaItems = new(this);
      VideoClips = new(this);
      VideoClipsGroups = new(this);
      Segments = new(this);
    }

    public Task InitAsync(IProgress<string> progress) {
      return Task.Run(() => {
        Sdb.AddDataAdapter(CategoryGroupsM.DataAdapter); // needs to be before People and Keywords
        Sdb.AddDataAdapter(KeywordsM.DataAdapter);
        Sdb.AddDataAdapter(Folders.DataAdapter); // needs to be before Viewers
        Sdb.AddDataAdapter(ViewersM.DataAdapter);
        Sdb.AddDataAdapter(PeopleM.DataAdapter); // needs to be before Segments
        Sdb.AddDataAdapter(GeoNames.DataAdapter);
        Sdb.AddDataAdapter(MediaItems.DataAdapter);
        Sdb.AddDataAdapter(VideoClipsGroups.DataAdapter); // needs to be before VideoClips
        Sdb.AddDataAdapter(VideoClips.DataAdapter);
        Sdb.AddDataAdapter(FavoriteFoldersM.DataAdapter);
        Sdb.AddDataAdapter(Segments.DataAdapter);

        Sdb.LoadAllTables(progress);
        Sdb.LinkReferences(progress);

        progress.Report("Loading Drives");
        Folders.AddDrives();
        progress.Report("Loading Folder Keywords");
        FolderKeywords.Load();
        progress.Report("Loading Ratings");
        Ratings.Load();

        // TODO better
        // cleanup
        Folders.AllDic.Clear();
        Folders.AllDic = null;
        GeoNames.AllDic.Clear();
        GeoNames.AllDic = null;
        KeywordsM.AllDic.Clear();
        KeywordsM.AllDic = null;
        MediaItems.AllDic.Clear();
        MediaItems.AllDic = null;
        PeopleM.AllDic.Clear();
        PeopleM.AllDic = null;
        VideoClips.AllDic.Clear();
        VideoClips.AllDic = null;
        Segments.AllDic.Clear();
        Segments.AllDic = null;

        Folders.IsExpanded = true;
      });
    }

    public bool CanViewerSeeThisFolder(Folder folder) => CurrentViewer?.CanSeeThisFolder(folder) != false;

    public bool CanViewerSeeContentOfThisFolder(Folder folder) => CurrentViewer?.CanSeeContentOfThisFolder(folder) != false;

    public bool CanViewerSee(MediaItem mediaItem) => CurrentViewer?.CanSee(mediaItem) != false;

    public void SetMediaItemSizesLoadedRange() {
      var zeroItems = MediaItems.ThumbsGrid == null || MediaItems.ThumbsGrid.FilteredItems.Count == 0;
      var min = zeroItems ? 0 : MediaItems.ThumbsGrid.FilteredItems.Min(x => x.Width * x.Height);
      var max = zeroItems ? 0 : MediaItems.ThumbsGrid.FilteredItems.Max(x => x.Width * x.Height);
      MediaItemSizes.Size.SetLoadedRange(min, max);
    }

    // get index for an item in DB in same order as it is in the tree
    public static int GetAllIndexBasedOnTreeOrder(List<IRecord> all, ICatTreeViewItem root, int treeIdx) {
      var allIdx = 0;
      if (all == null || root == null || treeIdx < 0) return allIdx;

      // if is item below
      if (treeIdx < root.Items.Count - 1) {
        allIdx = all.IndexOf(root.Items[treeIdx + 1] as IRecord);
        if (allIdx >= 0) return allIdx;
      }

      // if is item above
      if (treeIdx > 0) {
        allIdx = all.IndexOf(root.Items[treeIdx - 1] as IRecord);
        if (allIdx >= 0) return allIdx + 1;
      }

      return 0;
    }

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
