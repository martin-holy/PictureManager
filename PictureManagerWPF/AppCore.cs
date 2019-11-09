using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls;
using PictureManager.Dialogs;
using PictureManager.ShellStuff;
using PictureManager.Database;
using PictureManager.Properties;
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
    public Viewer CurrentViewer { get; set; }
    public double WindowsDisplayScale { get; set; }
    public double ThumbScale { get; set; } = 1.0;
    public ObservableCollection<LogItem> Log { get; set; } = new ObservableCollection<LogItem>();
    public HashSet<BaseTreeViewItem> ActiveFilterItems { get; } = new HashSet<BaseTreeViewItem>();

    private CancellationTokenSource _thumbCts;
    private Task _thumbTask;

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
      if (_thumbCts != null) {
        _thumbCts.Dispose();
        _thumbCts = null;
      }
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

    public void TreeView_Select(BaseTreeViewItem item, bool and, bool hide, bool recursive) {
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
            if (item.BackgroundBrush != BackgroundBrush.Default)
              SetBackgroundBrush(item, BackgroundBrush.Default);
            else {
              if (item is Rating && !and && !hide)
                SetBackgroundBrush(item, BackgroundBrush.OrThis);
              else {
                if (!and && !hide) SetBackgroundBrush(item, BackgroundBrush.OrThis);
                if (and && !hide) SetBackgroundBrush(item, BackgroundBrush.AndThis);
                if (!and && hide) SetBackgroundBrush(item, BackgroundBrush.Hidden);
              }
            }

            // reload with new filter
            MediaItems.ReapplyFilter();
          }

          break;
        }
        case Folder _:
        case FolderKeyword _: {
          if (item is Folder folder && !folder.IsAccessible) return;

          item.IsSelected = true;
          AppInfo.AppMode = AppMode.Browser;
          MediaItems.ScrollToTop();
          MediaItems.Load(item, recursive);
          LoadThumbnails(MediaItems.FilteredItems.ToArray());
          break;
        }
      }
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


    public Task CreateThumbnailAsync(MediaType type, string srcPath, string destPath, int size) {
      return type == MediaType.Image
        ? Task.Run(() => CreateImageThumbnail(srcPath, destPath, size))
        : CreateThumbnailAsync(srcPath, destPath, size);
    }

    public static void CreateImageThumbnail(string srcPath, string destPath, int desiredSize) {
      var dir = Path.GetDirectoryName(destPath);
      if (dir == null) return;
      Directory.CreateDirectory(dir);

      using (Stream srcFileStream = File.Open(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
        if (decoder.CodecInfo == null || !decoder.CodecInfo.FileExtensions.Contains("jpg") || decoder.Frames[0] == null) return;

        var frame = decoder.Frames[0];
        var orientation = (ushort?)((BitmapMetadata)frame.Metadata)?.GetQuery("System.Photo.Orientation") ?? 1;
        var rotated = orientation == (int)MediaOrientation.Rotate90 ||
                      orientation == (int)MediaOrientation.Rotate270;
        var pxw = (double)(rotated ? frame.PixelHeight : frame.PixelWidth);
        var pxh = (double)(rotated ? frame.PixelWidth : frame.PixelHeight);
        var size = MediaItems.GetThumbSize(pxw, pxh, desiredSize);
        var output = new TransformedBitmap(frame, new ScaleTransform(size.Width / pxw, size.Height / pxh, 0, 0));

        if (rotated) {
          // yes, angles 90 and 270 are switched
          var angle = orientation == (int)MediaOrientation.Rotate90 ? 270 : 90;
          output = new TransformedBitmap(output, new RotateTransform(angle));
        }

        var encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };
        encoder.Frames.Add(BitmapFrame.Create(output));

        using (Stream destFileStream = File.Open(destPath, FileMode.Create, FileAccess.ReadWrite)) {
          encoder.Save(destFileStream);
        }
      }
    }

    public Task CreateThumbnailAsync(string srcPath, string destPath, int size) {
      var dir = Path.GetDirectoryName(destPath);
      if (dir == null) return Task.CompletedTask;
      Directory.CreateDirectory(dir);

      var tcs = new TaskCompletionSource<bool>();
      var process = new Process {
        EnableRaisingEvents = true,
        StartInfo = new ProcessStartInfo {
          Arguments = $"src|\"{srcPath}\" dest|\"{destPath}\" quality|\"{80}\" size|\"{size}\"",
          FileName = "ThumbnailCreator.exe",
          UseShellExecute = false,
          CreateNoWindow = true
        }
      };

      process.Exited += (s, e) => {
        tcs.TrySetResult(true);
        process.Dispose();
      };

      process.Start();
      return tcs.Task;
    }

    private Task CreateThumbnailsAsync(MediaItem[] items, CancellationToken token) {
      return Task.Run(async () => {
        var count = items.Length;
        var workingOn = 0;

        await Task.WhenAll(
          from partition in Partitioner.Create(items).GetPartitions(Environment.ProcessorCount)
          select Task.Run(async delegate {
            using (partition) {
              while (partition.MoveNext()) {
                if (token.IsCancellationRequested) break;

                workingOn++;
                var workingOnInt = workingOn;
                Application.Current.Dispatcher.Invoke(delegate {
                  AppInfo.ProgressBarValueB = Convert.ToInt32((double) workingOnInt / count * 100);
                });

                var mi = partition.Current;
                if (mi == null) continue;
                if (File.Exists(mi.FilePathCache)) continue;

                await CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, Settings.Default.ThumbnailSize);

                mi.ReloadThumbnail();
              }
            }
          }));
      });
    }

    private Task<bool> ReadMetadataAndListThumbsAsync(MediaItem[] items, CancellationToken token) {
      return Task.Run(() => {
        var mediaItemsModifed = false;
        var count = items.Length;
        var workingOn = 0;

        foreach (var mi in items) {
          if (token.IsCancellationRequested) break;

          workingOn++;
          var percent = Convert.ToInt32((double) workingOn / count * 100);

          if (mi.IsNew) {
            mi.IsNew = false;

            Application.Current.Dispatcher.Invoke(delegate { AppInfo.MediaItemsCount++; });

            if (!mi.ReadMetadata()) {
              // delete corupted MediaItems
              Application.Current.Dispatcher.Invoke(delegate {
                MediaItems.LoadedItems.Remove(mi);
                MediaItems.FilteredItems.Remove(mi);
                MediaItems.Delete(mi);
                AppInfo.ProgressBarValueA = percent;
              });

              continue;
            }

            mi.SetThumbSize();
            mediaItemsModifed = true;
          }

          Application.Current.Dispatcher.Invoke(delegate {
            mi.SetInfoBox();
            MediaItems.SplitedItemsAdd(mi);
            AppInfo.ProgressBarValueA = percent;
          });
        }

        return mediaItemsModifed;
      });
    }

    public async void LoadThumbnails(MediaItem[] items) {
      // cancel previous work
      if (_thumbCts != null) {
        _thumbCts.Cancel();
        await _thumbTask;
      }

      AppInfo.ProgressBarIsIndeterminate = false;
      AppInfo.ProgressBarValueA = 0;
      AppInfo.ProgressBarValueB = 0;

      _thumbTask = new Task(async () => {
        _thumbCts?.Dispose();
        _thumbCts = new CancellationTokenSource();
        var token = _thumbCts.Token;

        // create thumbnails
        var thumbs = CreateThumbnailsAsync(items, token);
        // read metadata for new items and add thumbnails to grid
        var metadata = ReadMetadataAndListThumbsAsync(items, token);

        await Task.WhenAll(thumbs, metadata);

        var saveDb = metadata.Result;

        if (token.IsCancellationRequested) {
          saveDb = true;
          await Application.Current.Dispatcher.InvokeAsync(delegate {
            MediaItems.Delete(MediaItems.All.Where(x => x.IsNew).ToArray());
          });
        }

        if (saveDb)
          Sdb.SaveAllTables();
      });

      _thumbTask.Start();
      await _thumbTask;

      if (MediaItems.Current != null) {
        MediaItems.SetSelected(MediaItems.Current, false);
        MediaItems.SetSelected(MediaItems.Current, true);
      }

      MarkUsedKeywordsAndPeople();
      GC.Collect();
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
