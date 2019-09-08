using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MahApps.Metro.Controls;
using PictureManager.Dialogs;
using PictureManager.ShellStuff;
using PictureManager.Database;
using PictureManager.ViewModel;
using Directory = System.IO.Directory;

namespace PictureManager {
  public class AppCore {

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

    public SimpleDb Sdb { get; } = new SimpleDb();
    public MediaItems MediaItems { get; }
    public AppInfo AppInfo { get; } = new AppInfo();
    public Collection<BaseTreeViewTagItem> MarkedTags { get; } = new Collection<BaseTreeViewTagItem>();
    public BackgroundWorker ThumbsWorker { get; set; }
    public Viewer CurrentViewer { get; set; }
    public double WindowsDisplayScale { get; set; }
    public double ThumbScale { get; set; } = 1.0;
    public bool LastSelectedSourceRecursive { get; set; }
    public BaseTreeViewItem LastSelectedSource { get; set; }
    public ObservableCollection<LogItem> Log { get; set; } = new ObservableCollection<LogItem>();

    public volatile int ThumbProcessCounter;

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

    ~AppCore() {
      if (ThumbsWorker == null) return;
      ThumbsWorker.Dispose();
      ThumbsWorker = null;
    }

    public void Init() {
      WindowsDisplayScale = PresentationSource.FromVisual(App.WMain)?.CompositionTarget?.TransformToDevice.M11 * 100 ?? 100.0;

      Sdb.AddTable(CategoryGroups); // needs to be before People and Keywords
      Sdb.AddTable(Viewers); // needs to be before Folders
      Sdb.AddTable(People);
      Sdb.AddTable(Keywords);
      Sdb.AddTable(Folders);
      Sdb.AddTable(GeoNames);
      Sdb.AddTable(MediaItems);
      Sdb.AddTable(FavoriteFolders);

      Sdb.LoadAllTables();
      Sdb.LinkReferences();

      App.SplashScreen.AddMessage("Loading Folder Keywords");
      FolderKeywords.Load();
      App.SplashScreen.AddMessage("Loading Ratings");
      Ratings.Load();

      if (Viewers.Items.Count == 0) App.WMain.MenuViewers.Visibility = Visibility.Collapsed;

      AppInfo.MediaItemsCount = MediaItems.All.Count;
    }

    public void TreeView_Select(object item, bool and, bool hide, bool recursive, object sender = null) {
      if (item is BaseCategoryItem || item is CategoryGroup) {
        ((BaseTreeViewItem) item).IsSelected = false;
        return;
      }

      if (MediaItems.IsEditModeOn) {
        if (!(item is BaseTreeViewTagItem bti)) return;
        switch (item) {
          case Rating _:
          case Person _:
          case Keyword _:
          case GeoName _: {
            bti.IsMarked = !bti.IsMarked;
            if (bti.IsMarked)
              MarkedTags.Add(bti);
            else
              MarkedTags.Remove(bti);

            MediaItems.SetMetadata(item);

            MarkUsedKeywordsAndPeople();
            break;
          }
          default: {
            bti.IsSelected = false;
            break;
          }
        }
      }
      else {
        var bti = item as BaseTreeViewItem;
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
            if (bti == null) return;
            if (bti.BackgroundBrush != BackgroundBrush.Default)
              bti.BackgroundBrush = BackgroundBrush.Default;
            else {
              if (item is Rating && !and && !hide)
                bti.BackgroundBrush = BackgroundBrush.OrThis;
              else {
                if (!and && !hide) bti.BackgroundBrush = BackgroundBrush.OrThis;
                if (and && !hide) bti.BackgroundBrush = BackgroundBrush.AndThis;
                if (!and && hide) bti.BackgroundBrush = BackgroundBrush.Hidden;
              }
            }

            bti.IsSelected = false;
            if (LastSelectedSource != null) {
              LastSelectedSource.IsSelected = true;
              item = LastSelectedSource;
              recursive = LastSelectedSourceRecursive;
            }
            break;
          }
        }

        switch (item) {
          case Folder _:
          case FolderKeyword _: {
            if (item is Folder folder && !folder.IsAccessible) {
              folder.IsSelected = false;
              return;
            }

            LastSelectedSource = (BaseTreeViewItem) item;
            LastSelectedSource.IsSelected = true;
            LastSelectedSourceRecursive = recursive;

            AppInfo.AppMode = AppMode.Browser;
            MediaItems.ScrollToTop();
            MediaItems.Load(LastSelectedSource, recursive);
            LoadThumbnails();
            break;
          }
          default: {
            if (bti == null) return;
            bti.IsSelected = false;
            break;
          }
        }
      }
    }

    private void MarkedTagsAddWithIncrease(BaseTreeViewTagItem item) {
      item.PicCount++;
      if (item.IsMarked) return;
      item.IsMarked = true;
      MarkedTags.Add(item);
    }

    public void MarkUsedKeywordsAndPeople() {
      //can by Person, Keyword, FolderKeyword, Rating or GeoName

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

    public void LoadThumbnails() {
      AppInfo.ProgressBarIsIndeterminate = false;
      AppInfo.ProgressBarValue = 0;
      ThumbProcessCounter = 0;

      if (ThumbsWorker == null) {
        ThumbsWorker = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};

        ThumbsWorker.ProgressChanged += delegate(object sender, ProgressChangedEventArgs e) {
          AppInfo.ProgressBarValue = e.ProgressPercentage;
          if (((BackgroundWorker) sender).CancellationPending || e.UserState == null) return;
          MediaItems.SplitedItemsAdd((MediaItem) e.UserState);
        };

        ThumbsWorker.DoWork += delegate(object sender, DoWorkEventArgs e) {
          var worker = (BackgroundWorker) sender;
          var items = (List<MediaItem>) e.Argument;
          var count = items.Count;
          var done = 0;

          e.Result = false;

          foreach (var mi in items) {
            if (worker.CancellationPending) {
              e.Cancel = true;
              break;
            }

            if (mi.IsNew) {
              mi.IsNew = false;
              
              if (!mi.ReadMetadata()) { // delete corupted MediaItems
                Application.Current.Dispatcher.Invoke(delegate {
                  MediaItems.Items.Remove(mi);
                  MediaItems.Delete(mi);
                });

                done++;
                worker.ReportProgress(Convert.ToInt32(((double)done / count) * 100), null);

                continue;
              }

              mi.SetThumbSize();
              Application.Current.Dispatcher.Invoke(delegate { AppInfo.MediaItemsCount++; });
              e.Result = true;
            }

            if (!File.Exists(mi.FilePathCache)) {
              while (ThumbProcessCounter > 10) Thread.Sleep(100);
              CreateThumbnail(mi);
            }

            Application.Current.Dispatcher.Invoke(delegate { mi.SetInfoBox(); });

            done++;
            worker.ReportProgress(Convert.ToInt32(((double) done / count) * 100), mi);
          }
        };

        ThumbsWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e) {
          if (e.Cancelled) {
            // reason for cancelation was stop processing current MediaItems and start processing new MediaItems

            // remove new not processed media items
            MediaItems.Delete(MediaItems.All.Where(x => x.IsNew).ToArray());

            ThumbsWorker.RunWorkerAsync(MediaItems.Items.ToList());
            return;
          }

          if ((bool) e.Result)
            Sdb.SaveAllTables();

          if (MediaItems.Current != null) {
            MediaItems.SetSelected(MediaItems.Current, false);
            MediaItems.SetSelected(MediaItems.Current, true);
          }

          MarkUsedKeywordsAndPeople();
          GC.Collect();
        }; 
      }

      // worker will be started after cancel is done from RunWorkerCompleted
      if (ThumbsWorker.IsBusy) {
        ThumbsWorker.CancelAsync();
        return;
      }

      ThumbsWorker.RunWorkerAsync(MediaItems.Items.ToList());
    }

    public void SetMediaItemSizesLoadedRange() {
      var zeroItems = MediaItems.Items.Count == 0;
      var min = zeroItems ? 0 : MediaItems.Items.Min(x => x.Width * x.Height);
      var max = zeroItems ? 0 : MediaItems.Items.Max(x => x.Width * x.Height);
      MediaItemSizes.Size.SetLoadedRange(min, max);
    }

    public static FileOperationCollisionDialog.CollisionResult ShowFileOperationCollisionDialog(string srcFilePath, string destFilePath, Window owner, ref string fileName) {
      var result = FileOperationCollisionDialog.CollisionResult.Skip;
      var outFileName = fileName;

      Application.Current.Dispatcher.Invoke(delegate {
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

    public void CreateThumbnail(string srcPath, string destPath, int size) {
      CreateThumbnail(null, srcPath, destPath, size);
    }

    public void CreateThumbnail(MediaItem mi) {
      CreateThumbnail(mi, mi.FilePath, mi.FilePathCache, mi.ThumbSize);
    }

    public void CreateThumbnail(MediaItem mi, string srcPath, string destPath, int size) {
      var dir = Path.GetDirectoryName(destPath);
      if (dir == null) return;
      Directory.CreateDirectory(dir);

      var process = new Process {
        StartInfo = new ProcessStartInfo {
          Arguments = $"src|\"{srcPath}\" dest|\"{destPath}\" quality|\"{80}\" size|\"{size}\"",
          FileName = "ThumbnailCreator.exe",
          UseShellExecute = false,
          CreateNoWindow = true
        },
        EnableRaisingEvents = true
      };

      if (mi != null) {
        process.Exited += delegate {
          mi.ReloadThumbnail();
          ThumbProcessCounter--;
        };

        ThumbProcessCounter++;
      }

      process.Start();
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
