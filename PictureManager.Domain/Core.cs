using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PictureManager.Domain {
  public class Core {
    public string CachePath { get; set; }
    public int ThumbnailSize { get; set; }
    public double WindowsDisplayScale { get; set; }
    public ILogger Logger { get; set; }

    #region TreeView Roots and Categories
    public ObservableCollection<ICatTreeViewCategory> TreeViewCategories { get; }
    public FavoriteFolders FavoriteFolders { get; }
    public Folders Folders { get; }
    public Ratings Ratings { get; }
    public MediaItemSizes MediaItemSizes { get; }
    public People People { get; }
    public FolderKeywords FolderKeywords { get; }
    public Keywords Keywords { get; }
    public GeoNames GeoNames { get; }
    public Viewers Viewers { get; }
    public CategoryGroups CategoryGroups { get; }
    #endregion

    public SimpleDB.SimpleDB Sdb { get; private set; }
    public MediaItems MediaItems { get; }
    public VideoClips VideoClips { get; }
    public VideoClipsGroups VideoClipsGroups { get; }
    public Faces Faces { get; }
    public Collection<ICatTreeViewTagItem> MarkedTags { get; } = new();
    public Viewer CurrentViewer { get; set; }
    public double ThumbScale { get; set; } = 1.0;
    public HashSet<ICatTreeViewItem> ActiveFilterItems { get; } = new();

    private TaskScheduler UiTaskScheduler { get; }

    private Core() {
      UiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

      FavoriteFolders = new();
      Folders = new();
      Ratings = new();
      MediaItemSizes = new();
      People = new();
      FolderKeywords = new();
      Keywords = new();
      GeoNames = new();
      Viewers = new();

      TreeViewCategories = new() { FavoriteFolders, Folders, Ratings, MediaItemSizes, People, FolderKeywords, Keywords, GeoNames, Viewers };

      CategoryGroups = new();

      MediaItems = new();
      VideoClips = new();
      VideoClipsGroups = new();
      Faces = new();
    }

    public Task InitAsync(IProgress<string> progress) {
      return Task.Run(() => {
        Sdb = new(Logger);

        Sdb.AddTable(CategoryGroups);
        Sdb.AddTable(Folders); // needs to be before Viewers
        Sdb.AddTable(Viewers);
        Sdb.AddTable(People);
        Sdb.AddTable(Keywords);
        Sdb.AddTable(GeoNames);
        Sdb.AddTable(MediaItems);
        Sdb.AddTable(VideoClipsGroups); // needs to be before VideoClips
        Sdb.AddTable(VideoClips);
        Sdb.AddTable(FavoriteFolders);
        Sdb.AddTable(Faces, false);

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
        Keywords.AllDic.Clear();
        Keywords.AllDic = null;
        MediaItems.AllDic.Clear();
        MediaItems.AllDic = null;
        People.AllDic.Clear();
        People.AllDic = null;
        VideoClips.AllDic.Clear();
        VideoClips.AllDic = null;

        Folders.IsExpanded = true;
      });
    }

    public bool CanViewerSeeThisFolder(Folder folder) => CurrentViewer == null || CurrentViewer.CanSeeThisFolder(folder);

    public bool CanViewerSeeContentOfThisFolder(Folder folder) => CurrentViewer == null || CurrentViewer.CanSeeContentOfThisFolder(folder);

    public void SetMediaItemSizesLoadedRange() {
      var zeroItems = MediaItems.ThumbsGrid == null || MediaItems.ThumbsGrid.FilteredItems.Count == 0;
      var min = zeroItems ? 0 : MediaItems.ThumbsGrid.FilteredItems.Min(x => x.Width * x.Height);
      var max = zeroItems ? 0 : MediaItems.ThumbsGrid.FilteredItems.Max(x => x.Width * x.Height);
      MediaItemSizes.Size.SetLoadedRange(min, max);
    }

    public void MarkUsedKeywordsAndPeople() {
      //can be Person, Keyword, FolderKeyword, Rating or GeoName

      void MarkedTagsAddWithIncrease(ICatTreeViewTagItem item) {
        if (item == null) return;
        item.PicCount++;
        if (item.IsMarked) return;
        item.IsMarked = true;
        MarkedTags.Add(item);
      }

      // clear previous marked tags
      foreach (var item in MarkedTags) {
        item.IsMarked = false;
        item.PicCount = 0;
      }
      MarkedTags.Clear();

      if (MediaItems.ThumbsGrid == null) return;

      var mediaItems = MediaItems.ThumbsGrid.GetSelectedOrAll();
      foreach (var mi in mediaItems) {

        // People
        if (mi.People != null)
          foreach (var person in mi.People) {
            MarkedTagsAddWithIncrease(person);
            MarkedTagsAddWithIncrease(person.Parent as CategoryGroup);
          }

        // Keywords
        if (mi.Keywords != null) {
          foreach (var keyword in mi.Keywords) {
            var k = keyword;
            while (k != null) {
              MarkedTagsAddWithIncrease(k);
              MarkedTagsAddWithIncrease(k.Parent as CategoryGroup);
              k = k.Parent as Keyword;
            }
          }
        }

        // Folders
        var f = mi.Folder;
        while (f != null) {
          MarkedTagsAddWithIncrease(f);
          f = f.Parent as Folder;
        }

        // FolderKeywords
        var fk = mi.Folder.FolderKeyword;
        while (fk != null) {
          MarkedTagsAddWithIncrease(fk);
          fk = fk.Parent as FolderKeyword;
        }

        // GeoNames
        var gn = mi.GeoName;
        while (gn != null) {
          MarkedTagsAddWithIncrease(gn);
          gn = gn.Parent as GeoName;
        }

        // Ratings
        MarkedTagsAddWithIncrease(Ratings.GetRatingByValue(mi.Rating));
      }
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
