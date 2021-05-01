using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain {
  public class Core {
    public string CachePath { get; set; }
    public int ThumbnailSize { get; set; }
    public double WindowsDisplayScale { get; set; }
    public ILogger Logger { get; set; }

    #region TreeView Roots and Categories
    public ObservableCollection<BaseCategoryItem> TreeViewCategories { get; }
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
    public Collection<BaseTreeViewTagItem> MarkedTags { get; } = new Collection<BaseTreeViewTagItem>();
    public Viewer CurrentViewer { get; set; }
    public double ThumbScale { get; set; } = 1.0;
    public HashSet<BaseTreeViewItem> ActiveFilterItems { get; } = new HashSet<BaseTreeViewItem>();

    private TaskScheduler UiTaskScheduler { get; }

    private Core() {
      UiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

      FavoriteFolders = new FavoriteFolders();
      Folders = new Folders();
      Ratings = new Ratings();
      MediaItemSizes = new MediaItemSizes();
      People = new People { CanHaveGroups = true, CanModifyItems = true };
      FolderKeywords = new FolderKeywords();
      Keywords = new Keywords { CanHaveGroups = true, CanHaveSubItems = true, CanModifyItems = true };
      GeoNames = new GeoNames();
      Viewers = new Viewers { CanModifyItems = true };

      TreeViewCategories = new ObservableCollection<BaseCategoryItem>
        {FavoriteFolders, Folders, Ratings, MediaItemSizes, People, FolderKeywords, Keywords, GeoNames, Viewers};

      CategoryGroups = new CategoryGroups();

      MediaItems = new MediaItems();
      VideoClips = new VideoClips();
      VideoClipsGroups = new VideoClipsGroups();
    }

    public Task InitAsync(IProgress<string> progress) {
      return Task.Run(() => {
        Sdb = new SimpleDB.SimpleDB(Logger);

        Sdb.AddTable(CategoryGroups); // needs to be before People and Keywords
        Sdb.AddTable(Folders); // needs to be before Viewers
        Sdb.AddTable(Viewers);
        Sdb.AddTable(People);
        Sdb.AddTable(Keywords);
        Sdb.AddTable(GeoNames);
        Sdb.AddTable(MediaItems);
        Sdb.AddTable(VideoClips);
        Sdb.AddTable(VideoClipsGroups);
        Sdb.AddTable(FavoriteFolders);

        Sdb.LoadAllTables(progress);
        Sdb.LinkReferences(progress);

        progress.Report("Loading Drives");
        Folders.AddDrives();
        progress.Report("Loading Folder Keywords");
        FolderKeywords.Load();
        progress.Report("Loading Ratings");
        Ratings.Load();

        // cleanup
        Folders.AllDic.Clear();
        Folders.AllDic = null;
        MediaItems.AllDic.Clear();
        MediaItems.AllDic = null;
        VideoClips.AllDic.Clear();
        VideoClips.AllDic = null;

        Folders.IsExpanded = true;
      });
    }

    public bool CanViewerSeeThisFolder(Folder folder) {
      return CurrentViewer == null || CurrentViewer.CanSeeThisFolder(folder);
    }

    public bool CanViewerSeeContentOfThisFolder(Folder folder) {
      return CurrentViewer == null || CurrentViewer.CanSeeContentOfThisFolder(folder);
    }

    public void SetMediaItemSizesLoadedRange() {
      var zeroItems = MediaItems.ThumbsGrid.FilteredItems.Count == 0;
      var min = zeroItems ? 0 : MediaItems.ThumbsGrid.FilteredItems.Min(x => x.Width * x.Height);
      var max = zeroItems ? 0 : MediaItems.ThumbsGrid.FilteredItems.Max(x => x.Width * x.Height);
      MediaItemSizes.Size.SetLoadedRange(min, max);
    }

    public void MarkUsedKeywordsAndPeople() {
      //can by Person, Keyword, FolderKeyword, Rating or GeoName

      void MarkedTagsAddWithIncrease(BaseTreeViewTagItem item) {
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

      var mediaItems = MediaItems.ThumbsGrid.GetSelectedOrAll();
      foreach (var mi in mediaItems) {

        // People
        if (mi.People != null)
          foreach (var person in mi.People) {
            MarkedTagsAddWithIncrease(person);

            // Category Group
            if (!(person.Parent is CategoryGroup group)) continue;
            MarkedTagsAddWithIncrease(group);
          }

        // Keywords
        if (mi.Keywords != null) {
          foreach (var keyword in mi.Keywords) {
            var k = keyword;
            while (k != null) {
              MarkedTagsAddWithIncrease(k);

              // Category Group
              if (k.Parent is CategoryGroup group)
                MarkedTagsAddWithIncrease(group);

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

    private static Core _instance;
    private static readonly object Lock = new object();
    public static Core Instance {
      get {
        lock (Lock) {
          return _instance ?? (_instance = new Core());
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
