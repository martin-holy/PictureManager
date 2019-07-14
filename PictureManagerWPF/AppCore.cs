using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using System.Windows;
using PictureManager.Properties;
using PictureManager.ShellStuff;
using Directory = System.IO.Directory;

namespace PictureManager {
  public class AppCore : IDisposable {

    public Database.SimpleDB Sdb = new Database.SimpleDB();

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
    
    public ViewModel.FolderKeywords FolderKeywords { get; } = new ViewModel.FolderKeywords();
    public ViewModel.FavoriteFolders FavoriteFolders { get; } = new ViewModel.FavoriteFolders();
    public ViewModel.Ratings Ratings { get; } = new ViewModel.Ratings();
    public ViewModel.MediaItemSizes MediaItemSizes { get; } = new ViewModel.MediaItemSizes();

    public static WMain WMain => (WMain) Application.Current.Properties[nameof(AppProperty.WMain)];
    public static Collection<string> IncorrectChars { get; } = new Collection<string> {"\\", "/", ":", "*", "?", "\"", "<", ">", "|", ";"};
    public ViewModel.AppInfo AppInfo { get; } = new ViewModel.AppInfo();
    public Collection<ViewModel.BaseTreeViewTagItem> MarkedTags { get; } = new Collection<ViewModel.BaseTreeViewTagItem>();
    public BackgroundWorker ThumbsWorker { get; set; }
    public AutoResetEvent ThumbsResetEvent { get; set; } = new AutoResetEvent(false);
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
        if (ThumbsResetEvent != null) {
          ThumbsResetEvent.Dispose();
          ThumbsResetEvent = null;
        }
      }
      _disposed = true;
    }

    //TODO
    public void Init() {
      WindowsDisplayScale = PresentationSource.FromVisual(WMain)?.CompositionTarget?.TransformToDevice.M11 * 100 ?? 100.0;

      Sdb.AddTable(Viewers);
      Sdb.AddTable(People);
      Sdb.AddTable(Keywords);
      Sdb.AddTable(Folders);
      Sdb.AddTable(GeoNames);
      Sdb.AddTable(MediaItems);
      Sdb.AddTable(CategoryGroups);

      Sdb.LoadAllTables();
      Sdb.LinkReferences();

      if (Viewers.Items.Count == 0) WMain.MenuViewers.Visibility = Visibility.Collapsed;
      CurrentViewer = Viewers.Items.SingleOrDefault(x => x.Title == Settings.Default.Viewer) as Database.Viewer;



      // TODO
      App.SplashScreen.AddMessage("Loading Folder Keywords");
      FolderKeywords.Load();
      App.SplashScreen.AddMessage("Loading Favorite Folders");
      FavoriteFolders.Load();
      App.SplashScreen.AddMessage("Loading Ratings");
      Ratings.Load();


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
          case ViewModel.FavoriteFolder favoriteFolder: {
            //TODO
            /*var folder = Folders.ExpandTo(favoriteFolder.FullPath);
            if (folder != null) {
              var visibleTreeIndex = 0;
              Folders.GetVisibleTreeIndexFor(Folders.Items, folder, ref visibleTreeIndex);
              var offset = (FavoriteFolders.Items.Count + 1 + visibleTreeIndex) * 25;
              WMain.TvFoldersScrollViewer.ScrollToVerticalOffset(offset);
            }*/
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

            /*if (bti == null) return;
            bti.IsSelected = true;
            LastSelectedSource = bti;*/

            LastSelectedSource = (ViewModel.BaseTreeViewItem) item;
            LastSelectedSource.IsSelected = true;
            LastSelectedSourceRecursive = recursive;

            if (ThumbsWorker != null && ThumbsWorker.IsBusy) {
              ThumbsWorker.CancelAsync();
              ThumbsResetEvent.WaitOne();
            }

            AppInfo.AppMode = AppMode.Browser;
            MediaItems.Load(LastSelectedSource, recursive);
            MediaItems.ScrollTo(0);
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
            if (k.IsMarked) continue;
            k.IsMarked = true;
            MarkedTags.Add(k);
            k = k.Parent as Database.Keyword;
          }
        }

        // FolderKeywords
        if (mi.Folder.FolderKeyword != null && !mi.Folder.FolderKeyword.IsMarked) {
          var fk = mi.Folder.FolderKeyword;
          while (fk != null) {
            if (fk.IsMarked) continue;
            fk.IsMarked = true;
            MarkedTags.Add(fk);
            fk = fk.Parent as ViewModel.FolderKeyword;
          }
        }

        // GeoNames
        var gn = mi.GeoName;
        while (gn != null) {
          if (gn.IsMarked) continue;
          gn.IsMarked = true;
          MarkedTags.Add(gn);
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
            //fk.PicCount = mediaItems.Count(mi => mi.Folder.FolderKeyword != null && mi.Folder.FolderKeyword.FullPath.StartsWith(fk.FullPath));
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
          MediaItems.SplitedItemsAdd(MediaItems.Items[(int) e.UserState]);
          AppInfo.ProgressBarValue = e.ProgressPercentage;
        };

        ThumbsWorker.DoWork += delegate(object sender, DoWorkEventArgs e) {
          var worker = (BackgroundWorker) sender;
          var count = MediaItems.Items.Count;
          var done = 0;
          e.Result = e.Argument;

          foreach (var mi in MediaItems.Items) {
            if (worker.CancellationPending) {
              e.Cancel = true;
              ThumbsResetEvent.Set();
              break;
            }

            if (mi.IsNew) {
              mi.SaveMediaItemInToDb(false);
              mi.SetThumbSize();
              Application.Current.Properties[nameof(AppProperty.SubmitChanges)] = true;
            }

            if (!File.Exists(mi.FilePathCache))
              CreateThumbnail(mi.FilePath, mi.FilePathCache, mi.ThumbSize);

            if (mi.InfoBoxThumb.Count == 0)
              Application.Current.Dispatcher.Invoke(delegate { mi.SetInfoBox(); });

            done++;
            worker.ReportProgress(Convert.ToInt32(((double) done / count) * 100), mi.Index);
          }
        };

        ThumbsWorker.RunWorkerCompleted += delegate(object sender, RunWorkerCompletedEventArgs e) {
          if (((BackgroundWorker) sender).CancellationPending) return;

          if ((bool) Application.Current.Properties[nameof(AppProperty.SubmitChanges)]) {
            // TODO vymyslet co vsechno ulozit
          }

          if (MediaItems.Current != null) {
            MediaItems.Current.IsSelected = false;
            MediaItems.Current.IsSelected = true;
          }

          MarkUsedKeywordsAndPeople();
        }; 
      }

      Application.Current.Properties[nameof(AppProperty.SubmitChanges)] = false;
      ThumbsWorker.RunWorkerAsync();
    }

    public void SetMediaItemSizesLoadedRange() {
      var zeroItems = MediaItems.Items.Count == 0;
      var min = zeroItems ? 0 : MediaItems.Items.Min(x => x.Width * x.Height);
      var max = zeroItems ? 0 : MediaItems.Items.Max(x => x.Width * x.Height);
      MediaItemSizes.Size.SetLoadedRange(min, max);
    }

    #region File Operations
    public bool FileOperation(FileOperationMode mode, bool recycle) {
      return FileOperation(mode, null, null, null, recycle);
    }

    public bool FileOperation(FileOperationMode mode, string from, bool recycle) {
      return FileOperation(mode, from, null, null, recycle);
    }

    public bool FileOperation(FileOperationMode mode, string from, string to, string newName) {
      return FileOperation(mode, from, to, newName, true);
    }

    /// <summary>
    /// Operates only with selected MediaItems
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    public bool FileOperation(FileOperationMode mode, string to) {
      return FileOperation(mode, null, to, null, true);
    }

    public bool FileOperation(FileOperationMode mode, string from, string to, string newName, bool recycle) {
      Application.Current.Properties[nameof(AppProperty.FileOperationResult)] = new Dictionary<string, string>();
      //Copy, Move or delete selected MediaItems or folder
      using (var fo = new FileOperation(new PicFileOperationProgressSink())) {
        var flags = FileOperationFlags.FOF_NOCONFIRMMKDIR | (recycle
          ? FileOperationFlags.FOFX_RECYCLEONDELETE
          : FileOperationFlags.FOF_WANTNUKEWARNING);
        fo.SetOperationFlags(flags);
        if (from == null) { //MediaItems
          foreach (var mi in MediaItems.Items.Where(x => x.IsSelected)) {
            switch (mode) {
              case FileOperationMode.Copy: { fo.CopyItem(mi.FilePath, to, mi.FileName); break; }
              case FileOperationMode.Move: { fo.MoveItem(mi.FilePath, to, mi.FileName); break; }
              case FileOperationMode.Delete: { fo.DeleteItem(mi.FilePath); break; }
            }
          }
        } else { //Folders
          switch (mode) {
            case FileOperationMode.Copy: { fo.CopyItem(from, to, newName); break; }
            case FileOperationMode.Move: { fo.MoveItem(from, to, newName); break; }
            case FileOperationMode.Delete: { fo.DeleteItem(from); break; }
          }
        }

        fo.PerformOperations();
      }

      var foResult = (Dictionary<string, string>)Application.Current.Properties[nameof(AppProperty.FileOperationResult)];
      if (foResult.Count == 0) return false;

      //TODO check the foResult for list of items to work with insted of selected items
      //update DB and thumbnail cache
      using (var fo = new FileOperation()) {
        fo.SetOperationFlags(FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION |
                             FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOFX_KEEPNEWERFILE);

        if (mode == FileOperationMode.Delete) {
          var mediaItems = from == null
            ? MediaItems.Items.Where(x => x.IsSelected).ToList()
            : Folders.GetByPath(from)?.GetMediaItemsRecursive();

          if (mediaItems != null)
            foreach (var mi in mediaItems) {
              if (File.Exists(mi.FilePath)) continue; // TODO z foResultu se dozvim jesli mazat nebo ne

              MediaItems.Delete(mi);

              if (!File.Exists(mi.FilePathCache)) continue;
              fo.DeleteItem(mi.FilePathCache);
            }
        }

        if (mode == FileOperationMode.Copy || mode == FileOperationMode.Move) {
          foreach (var item in foResult) {
            var srcDir = Folders.GetByPath(Path.GetDirectoryName(item.Key));
            var destDir = Folders.GetByPath(Path.GetDirectoryName(item.Value));

            if (srcDir == null || destDir == null) continue;

            if (Database.MediaItems.SuportedExts.Any(ext => item.Value.EndsWith(ext, StringComparison.OrdinalIgnoreCase))) {
              if (!File.Exists(item.Value)) continue;
              
              var srcFile = srcDir.MediaItems.SingleOrDefault(x => x.FileName.Equals(Path.GetFileName(item.Key)));
              if (srcFile == null) continue;

              switch (mode) {
                case FileOperationMode.Copy: {
                  // duplicate MediaItem
                  var miCopy = srcFile.CopyTo(destDir, Path.GetFileName(item.Value));

                  // duplicate thumbnail
                  fo.CopyItem(srcFile.FilePathCache, destDir.FullPath, miCopy.FileName);
                  break;
                }
                case FileOperationMode.Move: {
                  //BUG: if the file already exists in the destination directory, FileOperation returns COPYENGINE_S_USER_IGNORED and source thumbnail file is not deleted

                  // take the path before is changed
                  var srcFilePathCache = srcFile.FilePathCache;
                  
                  // move MediaItem
                  srcFile.Folder.MediaItems.Remove(srcFile);
                  srcFile.Folder = destDir;
                  srcFile.Folder.MediaItems.Add(srcFile);
                  srcFile.FileName = Path.GetFileName(item.Value);

                  // move thumbnail
                  fo.MoveItem(srcFilePathCache, destDir.FullPath, srcFile.FileName);
                  break;
                }
              }
            } else {
              #region Move directories
              if (mode == FileOperationMode.Move) {
                // test if it is directory
                if (!Directory.Exists(item.Value)) continue;

                // take the path before is changed
                var srcFullPathCache = srcDir.FullPathCache;

                // move folder
                srcDir.Parent.Items.Remove(srcDir);
                srcDir.Parent = destDir;
                destDir.Items.Add(srcDir);

                // move thumbnails
                // TODO DEBUG destDir, jde o to jestli destDir je nadrazena slozka
                fo.MoveItem(srcFullPathCache, destDir.FullPathCache, destDir.Title);
              }
              #endregion
            }
          }
        }

        fo.PerformOperations();
      }

      return true;
    }
#endregion

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

    public bool CanViewerSeeThisDirectory(string path) {
      bool ok;
      if (CurrentViewer == null) return true;

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

    public static void CreateThumbnail(string oldPath, string newPath, int size) {
      var dir = Path.GetDirectoryName(newPath);
      if (dir == null) return;
      Directory.CreateDirectory(dir);
      Process process = null;

      try {
        process = new Process {
          StartInfo = new ProcessStartInfo {
            Arguments = $"src|\"{oldPath}\" dest|\"{newPath}\" quality|\"{80}\" size|\"{size}\"",
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

    public static List<string> GetAllDirectoriesSafely(string path) {
      var list = new List<string>();
      foreach (var dir in Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly)) {
        try {
          list.AddRange(GetAllDirectoriesSafely(dir));
          list.Add(dir);
        }
        catch {
          // 
        }
      }
      return list;
    }
  }
}
