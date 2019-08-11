using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Windows;
using PictureManager.Dialogs;
using PictureManager.Properties;
using PictureManager.ShellStuff;
using Directory = System.IO.Directory;

namespace PictureManager {
  public class AppCore : IDisposable {

    public Database.SimpleDb Sdb = new Database.SimpleDb();

    public ObservableCollection<ViewModel.BaseTreeViewItem> FoldersRoot { get; } = new ObservableCollection<ViewModel.BaseTreeViewItem>();
    public ObservableCollection<ViewModel.BaseTreeViewItem> KeywordsRoot { get; } = new ObservableCollection<ViewModel.BaseTreeViewItem>();
    public ObservableCollection<ViewModel.BaseTreeViewItem> FiltersRoot { get; } = new ObservableCollection<ViewModel.BaseTreeViewItem>();

    public Database.Folders Folders { get; } = new Database.Folders();
    public Database.MediaItems MediaItems { get; set; } = new Database.MediaItems();
    public Database.People People { get; } = new Database.People { CanHaveGroups = true, CanModifyItems = true };
    public Database.Keywords Keywords { get; } = new Database.Keywords {CanHaveGroups = true, CanHaveSubItems = true, CanModifyItems = true};
    public Database.GeoNames GeoNames { get; } = new Database.GeoNames();
    public Database.Viewers Viewers { get; } = new Database.Viewers {CanModifyItems = true};
    public Database.CategoryGroups CategoryGroups { get; } = new Database.CategoryGroups();
    public Database.FavoriteFolders FavoriteFolders { get; } = new Database.FavoriteFolders();

    public ViewModel.FolderKeywords FolderKeywords { get; } = new ViewModel.FolderKeywords();
    
    public ViewModel.Ratings Ratings { get; } = new ViewModel.Ratings();
    public ViewModel.MediaItemSizes MediaItemSizes { get; } = new ViewModel.MediaItemSizes();

    public static WMain WMain => (WMain) Application.Current.Properties[nameof(AppProperty.WMain)];
    public static Dictionary<string, string> FileOperationResult => (Dictionary<string, string>) Application.Current.Properties[nameof(AppProperty.FileOperationResult)];
    public static Collection<string> IncorrectChars { get; } = new Collection<string> {"\\", "/", ":", "*", "?", "\"", "<", ">", "|", ";", "\n"};
    public ViewModel.AppInfo AppInfo { get; } = new ViewModel.AppInfo();
    public Collection<ViewModel.BaseTreeViewTagItem> MarkedTags { get; } = new Collection<ViewModel.BaseTreeViewTagItem>();
    public BackgroundWorker ThumbsWorker { get; set; }
    public Database.Viewer CurrentViewer { get; set; }
    public double WindowsDisplayScale { get; set; }
    public double ThumbScale { get; set; } = 1.0;

    private bool _disposed;
    private ViewModel.BaseTreeViewItem _lastSelectedSource;

    public bool LastSelectedSourceRecursive { get; set; }
    public ViewModel.BaseTreeViewItem LastSelectedSource {
      get => _lastSelectedSource;
      set {
        if (_lastSelectedSource == value) return;
        if (_lastSelectedSource != null)
          _lastSelectedSource.IsSelected = false;
        _lastSelectedSource = value;
      }
    }

    public AppCore() {
      Application.Current.Properties[nameof(AppProperty.FileOperationResult)] = new Dictionary<string, string>();
    }

    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
      if (_disposed) return;
      if (disposing) {
        if (ThumbsWorker != null) {
          ThumbsWorker.Dispose();
          ThumbsWorker = null;
        }
      }
      _disposed = true;
    }

    public void Init() {
      WindowsDisplayScale = PresentationSource.FromVisual(WMain)?.CompositionTarget?.TransformToDevice.M11 * 100 ?? 100.0;

      Sdb.AddTable(CategoryGroups); // needs to be before People and Keywords
      Sdb.AddTable(Viewers);
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

      if (Viewers.Items.Count == 0) WMain.MenuViewers.Visibility = Visibility.Collapsed;
      CurrentViewer = Viewers.Items.SingleOrDefault(x => x.Title == Settings.Default.Viewer) as Database.Viewer;

      FoldersRoot.Add(FavoriteFolders);
      FoldersRoot.Add(Folders);
      KeywordsRoot.Add(Ratings);
      KeywordsRoot.Add(MediaItemSizes);
      KeywordsRoot.Add(People);
      KeywordsRoot.Add(FolderKeywords);
      KeywordsRoot.Add(Keywords);
      KeywordsRoot.Add(GeoNames);
      FiltersRoot.Add(Viewers);
    }

    public void UpdateStatusBarInfo() {
      AppInfo.PositionSlashCount = MediaItems.Current == null
        ? MediaItems.Items.Count.ToString()
        : $"{MediaItems.Current.Index + 1}/{MediaItems.Items.Count}";
      AppInfo.Selected = MediaItems.Items.Count(x => x.IsSelected);
      AppInfo.Modifed = MediaItems.IsEditModeOn ? MediaItems.Items.Count(x => x.IsModifed) : 0;
      AppInfo.CurrentMediaItem = MediaItems.Current;
    }

    public void TreeView_Select(object item, bool and, bool hide, bool recursive) {
      if (item is ViewModel.BaseCategoryItem || item is Database.CategoryGroup) return;

      if (MediaItems.IsEditModeOn) {
        if (!(item is ViewModel.BaseTreeViewTagItem bti)) return;
        switch (item) {
          case ViewModel.Rating _:
          case Database.Person _:
          case Database.Keyword _:
          case Database.GeoName _: {
            bti.IsMarked = !bti.IsMarked;
            if (bti.IsMarked)
              MarkedTags.Add(bti);
            else
              MarkedTags.Remove(bti);

            MediaItems.EditMetadata(item);

            MarkUsedKeywordsAndPeople();
            UpdateStatusBarInfo();
            break;
          }
          default: {
            bti.IsSelected = false;
            break;
          }
        }
      }
      else {
        var bti = item as ViewModel.BaseTreeViewItem;
        switch (item) {
          case Database.FavoriteFolder favoriteFolder: {
            Folders.ExpandTo(favoriteFolder.Folder);
            var visibleTreeIndex = 0;
            Folders.GetVisibleTreeIndexFor(Folders.Items, favoriteFolder.Folder, ref visibleTreeIndex);
            var offset = (FavoriteFolders.Items.Count + 1 + visibleTreeIndex) * 25;
            WMain.TvFoldersScrollViewer.ScrollToVerticalOffset(offset);
            break;
          }
          case ViewModel.Rating _:
          case Database.Person _:
          case Database.Keyword _:
          case Database.GeoName _: {
            if (bti == null) return;
            if (bti.BackgroundBrush != BackgroundBrush.Default)
              bti.BackgroundBrush = BackgroundBrush.Default;
            else {
              if (item is ViewModel.Rating && !and && !hide)
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
          case Database.Folder _:
          case ViewModel.FolderKeyword _: {
            if (item is Database.Folder folder && !folder.IsAccessible) {
              folder.IsSelected = false;
              return;
            }

            LastSelectedSource = (ViewModel.BaseTreeViewItem) item;
            LastSelectedSource.IsSelected = true;
            LastSelectedSourceRecursive = recursive;

            AppInfo.AppMode = AppMode.Browser;
            MediaItems.ScrollTo(0);
            MediaItems.Load(LastSelectedSource, recursive);
            LoadThumbnails();
            GC.Collect();
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

        // TODO DEBUG asi tam nemusi bejt Where(...)
        // People
        foreach (var person in mi.People.Where(person => !person.IsMarked)) {
          person.IsMarked = true;
          MarkedTags.Add(person);
        }

        // Keywords
        foreach (var keyword in mi.Keywords) {
          var k = keyword;
          while (k != null) {
            if (!k.IsMarked) {
              k.IsMarked = true;
              MarkedTags.Add(k);
            }
            k = k.Parent as Database.Keyword;
          }
        }

        // FolderKeywords
        if (mi.Folder.FolderKeyword != null && !mi.Folder.FolderKeyword.IsMarked) {
          var fk = mi.Folder.FolderKeyword;
          while (fk != null) {
            if (!fk.IsMarked) {
              fk.IsMarked = true;
              MarkedTags.Add(fk);
            }
            fk = fk.Parent as ViewModel.FolderKeyword;
          }
        }

        // GeoNames
        var gn = mi.GeoName;
        while (gn != null) {
          if (!gn.IsMarked) {
            gn.IsMarked = true;
            MarkedTags.Add(gn);
          }
          gn = gn.Parent as Database.GeoName;
        }
      }

      // Ratings
      foreach (var rating in mediaItems.Select(p => p.Rating).Distinct().Select(r => Ratings.GetRatingByValue(r))) {
        rating.IsMarked = true;
        MarkedTags.Add(rating);
      }

      // set count of MediaItems
      foreach (var item in MarkedTags) {
        switch (item) {
          case Database.Person p: {
            p.PicCount = mediaItems.Count(mi => mi.People.Contains(p));
            break;
          }
          case Database.Keyword k: {
            k.PicCount = mediaItems.Count(mi => mi.Keywords.Any(x => x.FullPath.StartsWith(k.FullPath)));
            break;
          }
          case ViewModel.FolderKeyword fk: {
            //TODO
            fk.PicCount = mediaItems.Count(mi => fk.Folders.Contains(mi.Folder));
            break;
          }
          case ViewModel.Rating r: {
            r.PicCount = mediaItems.Count(mi => mi.Rating == r.Value);
            break;
          }
          case Database.GeoName g: {
            g.PicCount = mediaItems.Count(mi => mi.GeoName == g);
            break;
          }
        }
      }

      foreach (var pg in People.Items.OfType<Database.CategoryGroup>()) {
        pg.PicCount = pg.Items.Cast<Database.Person>().Sum(x => x.PicCount);
        pg.IsMarked = pg.PicCount > 0;
      }

      foreach (var kg in Keywords.Items.OfType<Database.CategoryGroup>()) {
        kg.PicCount = kg.Items.Cast<Database.Keyword>().Sum(x => x.PicCount);
        kg.IsMarked = kg.PicCount > 0;
      }

      foreach (var g in MarkedTags.OfType<Database.GeoName>()) {
        var parent = g.Parent as Database.GeoName;
        while (parent != null) {
          parent.PicCount = parent.Items.Cast<Database.GeoName>().Sum(x => x.PicCount);
          parent = parent.Parent as Database.GeoName;
        }
      }
    }

    public void LoadThumbnails() {
      AppInfo.ProgressBarValue = 0;

      if (ThumbsWorker == null) {
        ThumbsWorker = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};

        ThumbsWorker.ProgressChanged += delegate(object sender, ProgressChangedEventArgs e) {
          if (((BackgroundWorker) sender).CancellationPending || e.UserState == null) return;
          MediaItems.SplitedItemsAdd((int) e.UserState);
          AppInfo.ProgressBarValue = e.ProgressPercentage;
        };

        ThumbsWorker.DoWork += delegate(object sender, DoWorkEventArgs e) {
          var worker = (BackgroundWorker) sender;
          var items = (List<Database.BaseMediaItem>) e.Argument;
          var count = items.Count;
          var done = 0;
          e.Result = e.Argument;

          foreach (var mi in items) {
            if (worker.CancellationPending) {
              e.Cancel = true;
              break;
            }

            if (mi.IsNew) {
              mi.IsNew = false;
              mi.ReadMetadata();
              mi.SetThumbSize();
              Application.Current.Properties[nameof(AppProperty.SubmitChanges)] = true;
            }

            if (!mi.IsCorupted && !File.Exists(mi.FilePathCache))
              CreateThumbnail(mi.FilePath, mi.FilePathCache, mi.ThumbSize);

            if (mi.InfoBoxThumb.Count == 0)
              Application.Current.Dispatcher.Invoke(delegate { mi.SetInfoBox(); });

            done++;
            worker.ReportProgress(Convert.ToInt32(((double) done / count) * 100), mi.Index);
          }
        };

        ThumbsWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e) {
          // delete corupted MediaItems
          foreach (var mi in ((List<Database.BaseMediaItem>) e.Result).Where(x => x.IsCorupted))
            MediaItems.Delete(mi);

          if (e.Cancelled) {
            // reason for cancelation was stop processing current MediaItems and start processing new MediaItems
            ThumbsWorker.RunWorkerAsync(MediaItems.Items.ToList());
            return;
          }

          if ((bool) Application.Current.Properties[nameof(AppProperty.SubmitChanges)])
            Sdb.SaveAllTables();

          if (MediaItems.Current != null) {
            MediaItems.Current.IsSelected = false;
            MediaItems.Current.IsSelected = true;
          }

          MarkUsedKeywordsAndPeople();
        }; 
      }

      // worker will be started after cancel is done from RunWorkerCompleted
      if (ThumbsWorker.IsBusy) {
        ThumbsWorker.CancelAsync();
        return;
      }

      Application.Current.Properties[nameof(AppProperty.SubmitChanges)] = false;
      ThumbsWorker.RunWorkerAsync(MediaItems.Items.ToList());
    }

    public void SetMediaItemSizesLoadedRange() {
      var zeroItems = MediaItems.Items.Count == 0;
      var min = zeroItems ? 0 : MediaItems.Items.Min(x => x.Width * x.Height);
      var max = zeroItems ? 0 : MediaItems.Items.Max(x => x.Width * x.Height);
      MediaItemSizes.Size.SetLoadedRange(min, max);
    }

    public FileOperationCollisionDialog.CollisionResult ShowFileOperationCollisionDialog(string srcFilePath, string destFilePath, Window owner, ref string fileName) {
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

    public Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent) {
      FileOperationResult.Clear();
      using (var fo = new FileOperation(new PicFileOperationProgressSink())) {
        fo.SetOperationFlags(
          (recycle ? FileOperationFlags.FOFX_RECYCLEONDELETE : FileOperationFlags.FOF_WANTNUKEWARNING) |
          (silent
            ? FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION |
              FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOFX_KEEPNEWERFILE
            : FileOperationFlags.FOF_NOCONFIRMMKDIR));

        items.ForEach(x => fo.DeleteItem(x));
        fo.PerformOperations();
      }

      return FileOperationResult;
    }

    public bool CanViewerSeeThisFile(string filePath) {
      bool ok;
      if (CurrentViewer == null) return true;

      var incFo = CurrentViewer.IncludedFolders.Items.Select(x => x.ToolTip).ToArray();
      var excFo = CurrentViewer.ExcludedFolders.Items.Select(x => x.ToolTip).ToArray();
      var incFi = new string[0];
      var excFi = new string[0];

      if (incFo.Any(x => filePath.StartsWith(x, StringComparison.OrdinalIgnoreCase))) {
        if (excFo.Any(x => filePath.StartsWith(x, StringComparison.OrdinalIgnoreCase))) {
          ok = incFi.Any(x => filePath.Equals(x, StringComparison.OrdinalIgnoreCase));
        } else {
          ok = !excFi.Any(x => filePath.Equals(x, StringComparison.OrdinalIgnoreCase));
        }
      } else {
        ok = incFi.Any(x => filePath.Equals(x, StringComparison.OrdinalIgnoreCase));
      }

      return ok;
    }

    // TODO predelat na objekty
    public bool CanViewerSeeThisDirectory(Database.Folder folder) {
      if (CurrentViewer == null) return true;

      var path = folder.FullPath;
      bool ok;
      var incFo = CurrentViewer.IncludedFolders.Items.Select(x => x.ToolTip).ToArray();
      var excFo = CurrentViewer.ExcludedFolders.Items.Select(x => x.ToolTip).ToArray();
      var incFi = new string[0];
      var excFi = new string[0];

      if (incFo.Any(x => x.Contains(path)) || incFo.Any(path.Contains)) {
        if (excFo.Any(x => x.Contains(path)) || excFo.Any(path.Contains)) {
          ok = incFi.Any(x => x.StartsWith(path));
        } else {
          ok = !excFi.Any(x => x.StartsWith(path));
        }
      } else {
        ok = incFi.Any(x => x.StartsWith(path));
      }

      return ok;
    }

    public static void CreateThumbnail(string srcPath, string destPath, int size) {
      var dir = Path.GetDirectoryName(destPath);
      if (dir == null) return;
      Directory.CreateDirectory(dir);
      Process process = null;

      try {
        process = new Process {
          StartInfo = new ProcessStartInfo {
            Arguments = $"src|\"{srcPath}\" dest|\"{destPath}\" quality|\"{80}\" size|\"{size}\"",
            FileName = "ThumbnailCreator.exe",
            UseShellExecute = false,
            CreateNoWindow = true
          }
        };
        process.Start();
        process.WaitForExit(2000);
      }
      finally {
        process?.Dispose();
      }
    }

    public static void ShowErrorDialog(Exception ex) {
      MessageBox.Show($"{ex.Message}\n{ex.StackTrace}");
    }
  }
}
