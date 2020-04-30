using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;
using PictureManager.Dialogs;
using PictureManager.ShellStuff;
using PictureManager.Database;
using PictureManager.ViewModel;
using SimpleDB;

namespace PictureManager {
  public class AppCore: ILogger {

    #region TreeView Roots and Categories
    // Folders
    public ObservableCollection<BaseTreeViewItem> FoldersRoot { get; }
    public FavoriteFolders FavoriteFolders { get; }
    public Folders Folders { get; }
    // Keywords
    public ObservableCollection<BaseTreeViewItem> KeywordsRoot { get; }
    public Ratings Ratings { get; }
    public MediaItemSizes MediaItemSizes { get; }
    public People People { get; }
    public FolderKeywords FolderKeywords { get; }
    public Keywords Keywords { get; }
    public GeoNames GeoNames { get; }
    // Filters
    public ObservableCollection<BaseTreeViewItem> FiltersRoot { get; }
    public Viewers Viewers { get; }

    public CategoryGroups CategoryGroups { get; }
    #endregion

    public SimpleDB.SimpleDB Sdb { get; private set; }
    public MediaItems MediaItems { get; }
    public AppInfo AppInfo { get; } = new AppInfo();
    public Collection<BaseTreeViewTagItem> MarkedTags { get; } = new Collection<BaseTreeViewTagItem>();
    public Viewer CurrentViewer { get; set; }
    public double WindowsDisplayScale { get; set; }
    public double ThumbScale { get; set; } = 1.0;
    public ObservableCollection<LogItem> Log { get; set; } = new ObservableCollection<LogItem>();
    public HashSet<BaseTreeViewItem> ActiveFilterItems { get; } = new HashSet<BaseTreeViewItem>();

    public AppCore() {
      #region TreeView Roots and Categories
      // Folders
      FavoriteFolders = new FavoriteFolders();
      Folders = new Folders();
      FoldersRoot = new ObservableCollection<BaseTreeViewItem> {FavoriteFolders, Folders};
      // Keywords
      Ratings = new Ratings();
      MediaItemSizes = new MediaItemSizes();
      People = new People {CanHaveGroups = true, CanModifyItems = true};
      FolderKeywords = new FolderKeywords();
      Keywords = new Keywords {CanHaveGroups = true, CanHaveSubItems = true, CanModifyItems = true};
      GeoNames = new GeoNames();
      KeywordsRoot = new ObservableCollection<BaseTreeViewItem> {Ratings, MediaItemSizes, People, FolderKeywords, Keywords, GeoNames};
      // Filters
      Viewers = new Viewers {CanModifyItems = true};
      FiltersRoot = new ObservableCollection<BaseTreeViewItem> {Viewers};

      CategoryGroups = new CategoryGroups();
      #endregion

      MediaItems = new MediaItems();
    }

    public Task InitAsync(IProgress<string> progress) {
      return Task.Run(() => {
        Sdb = new SimpleDB.SimpleDB(this);

        Sdb.AddTable(CategoryGroups); // needs to be before People and Keywords
        Sdb.AddTable(Folders); // needs to be before Viewers
        Sdb.AddTable(Viewers);
        Sdb.AddTable(People);
        Sdb.AddTable(Keywords);
        Sdb.AddTable(GeoNames);
        Sdb.AddTable(MediaItems);
        Sdb.AddTable(FavoriteFolders);

        Sdb.LoadAllTables(progress);
        Sdb.LinkReferences(progress);

        progress.Report("Loading Drives");
        Folders.AddDrives();
        progress.Report("Loading Folder Keywords");
        FolderKeywords.Load();
        progress.Report("Loading Ratings");
        Ratings.Load();

        AppInfo.MediaItemsCount = MediaItems.All.Count;
        AppInfo.ProgressBarValueA = 100;
        AppInfo.ProgressBarValueB = 100;
        Folders.IsExpanded = true;
      });
    }

    public bool CanViewerSeeThisFolder(Folder folder) {
      return CurrentViewer == null || CurrentViewer.CanSeeThisFolder(folder);
    }

    public bool CanViewerSeeContentOfThisFolder(Folder folder) {
      return CurrentViewer == null || CurrentViewer.CanSeeContentOfThisFolder(folder);
    }

    public void SetBackgroundBrush(BaseTreeViewItem item, BackgroundBrush backgroundBrush) {
      item.BackgroundBrush = backgroundBrush;
      if (backgroundBrush == BackgroundBrush.Default)
        ActiveFilterItems.Remove(item);
      else
        ActiveFilterItems.Add(item);

      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterAndCount));
      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterOrCount));
      AppInfo.OnPropertyChanged(nameof(AppInfo.FilterHiddenCount));
    }

    public async void TreeView_Select(BaseTreeViewItem item, bool and, bool hide, bool recursive) {
      if (item == null) return;

      switch (item) {
        case FavoriteFolder favoriteFolder: {
          if (favoriteFolder.Folder.IsThisOrParentHidden()) return;
          BaseTreeViewItem.ExpandTo(favoriteFolder.Folder);

          // scroll to folder
          var visibleTreeIndex = 0;
          Folders.GetVisibleTreeIndexFor(Folders.Items, favoriteFolder.Folder, ref visibleTreeIndex);
          var offset = (FavoriteFolders.Items.Count + visibleTreeIndex) * 25;
          var border = VisualTreeHelper.GetChild(App.WMain.TvFolders, 0);
          var scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
          scrollViewer?.ScrollToVerticalOffset(offset);
          break;
        }
        case Rating _:
        case Person _:
        case Keyword _:
        case GeoName _: {
          if (MediaItems.IsEditModeOn) {
            if (!(item is BaseTreeViewTagItem bti)) return;

            bti.IsMarked = !bti.IsMarked;
            if (bti.IsMarked)
              MarkedTags.Add(bti);
            else {
              MarkedTags.Remove(bti);
              bti.PicCount = 0;
            }

            MediaItems.SetMetadata(item);

            MarkUsedKeywordsAndPeople();
          }
          else {
            // get items by tag
            List<MediaItem> items = null;

            switch ((BaseTreeViewTagItem)item) {
              case Rating rating: items = MediaItems.All.Where(x => x.Rating == rating.Value).ToList(); break;
              case Keyword keyword: items = keyword.GetMediaItems(recursive).ToList(); break;
              case Person person: items = person.MediaItems; break;
              case GeoName geoName: items = geoName.GetMediaItems(recursive).ToList(); break;
            }
            await MediaItems.LoadAsync(items, null);
            MarkUsedKeywordsAndPeople();
          }

          break;
        }
        case Folder _:
        case FolderKeyword _: {
          if (item is Folder folder && !folder.IsAccessible) return;

          item.IsSelected = true;

          if (AppInfo.AppMode == AppMode.Viewer) {
            App.WMain.SwitchToBrowser();
          }

          var roots = (item as FolderKeyword)?.Folders ?? new List<Folder> {(Folder) item};
          var folders = Folder.GetFolders(roots, recursive);
          
          await MediaItems.LoadAsync(null, folders);
          MarkUsedKeywordsAndPeople();
          break;
        }
      }
    }

    public void ActivateFilter(BaseTreeViewItem item, BackgroundBrush mode) {
      SetBackgroundBrush(item, item.BackgroundBrush != BackgroundBrush.Default ? BackgroundBrush.Default : mode);

      // reload with new filter
      MediaItems.ReapplyFilter();
    }

    public void ClearFilters() {
      foreach (var item in ActiveFilterItems.ToArray())
        SetBackgroundBrush(item, BackgroundBrush.Default);

      // reload with new filter
      MediaItems.ReapplyFilter();
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

      var mediaItems = MediaItems.GetSelectedOrAll();
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

    public void SetMediaItemSizesLoadedRange() {
      var zeroItems = MediaItems.FilteredItems.Count == 0;
      var min = zeroItems ? 0 : MediaItems.FilteredItems.Min(x => x.Width * x.Height);
      var max = zeroItems ? 0 : MediaItems.FilteredItems.Max(x => x.Width * x.Height);
      MediaItemSizes.Size.SetLoadedRange(min, max);
    }

    public static FileOperationCollisionDialog.CollisionResult ShowFileOperationCollisionDialog(string srcFilePath, string destFilePath, Window owner, ref string fileName) {
      var result = FileOperationCollisionDialog.CollisionResult.Skip;
      var outFileName = fileName;

      Application.Current.Dispatcher?.Invoke(delegate {
        var focd = new FileOperationCollisionDialog(srcFilePath, destFilePath, owner);
        focd.ShowDialog();
        result = focd.Result;
        outFileName = focd.FileName;
      });

      fileName = outFileName;

      return result;
    }

    public static Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent) {
      var fops = new PicFileOperationProgressSink();
      using (var fo = new FileOperation(fops)) {
        fo.SetOperationFlags(
          (recycle ? FileOperationFlags.FOFX_RECYCLEONDELETE : FileOperationFlags.FOF_WANTNUKEWARNING) |
          (silent
            ? FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION |
              FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOFX_KEEPNEWERFILE
            : FileOperationFlags.FOF_NOCONFIRMMKDIR));

        items.ForEach(x => fo.DeleteItem(x));
        fo.PerformOperations();
      }

      return fops.FileOperationResult;
    }

    public void LogError(Exception ex) {
      LogError(ex, string.Empty);
    }

    public void LogError(Exception ex, string msg) {
      Application.Current.Invoke(delegate {
        Log.Add(new LogItem(string.IsNullOrEmpty(msg) ? ex.Message : msg, $"{msg}\n{ex.Message}\n{ex.StackTrace}"));
      });
    }
  }
}
