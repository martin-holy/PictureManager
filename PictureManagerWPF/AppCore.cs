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

    public ObservableCollection<ViewModel.BaseTreeViewItem> FoldersRoot { get; } = new ObservableCollection<ViewModel.BaseTreeViewItem>();
    public ObservableCollection<ViewModel.BaseTreeViewItem> KeywordsRoot { get; } = new ObservableCollection<ViewModel.BaseTreeViewItem>();
    public ObservableCollection<ViewModel.BaseTreeViewItem> FiltersRoot { get; } = new ObservableCollection<ViewModel.BaseTreeViewItem>();

    public ViewModel.Keywords Keywords { get; } = new ViewModel.Keywords {CanHaveGroups = true, CanHaveSubItems = true, CanModifyItems = true};
    public ViewModel.People People { get; } = new ViewModel.People {CanHaveGroups = true, CanModifyItems = true};
    public ViewModel.FolderKeywords FolderKeywords { get; } = new ViewModel.FolderKeywords();
    public ViewModel.Folders Folders { get; } = new ViewModel.Folders();
    public ViewModel.FavoriteFolders FavoriteFolders { get; } = new ViewModel.FavoriteFolders();
    public ViewModel.Ratings Ratings { get; } = new ViewModel.Ratings();
    public ViewModel.Filters Filters { get; } = new ViewModel.Filters();
    public ViewModel.Viewers Viewers { get; } = new ViewModel.Viewers {CanModifyItems = true};
    public ViewModel.GeoNames GeoNames { get; } = new ViewModel.GeoNames();
    public ViewModel.SqlQueries SqlQueries { get; } = new ViewModel.SqlQueries {CanHaveGroups = true, CanModifyItems = true};

    public static WMain WMain => (WMain) Application.Current.Properties[nameof(AppProperty.WMain)];
    public static Collection<string> IncorrectChars { get; } = new Collection<string> {"\\", "/", ":", "*", "?", "\"", "<", ">", "|", ";"};
    public ViewModel.AppInfo AppInfo { get; } = new ViewModel.AppInfo();
    public DataModel.PmDataContext Db { get; set; } = new DataModel.PmDataContext("Data Source = data.db");
    public ViewModel.MediaItems MediaItems { get; set; } = new ViewModel.MediaItems();
    public Collection<ViewModel.BaseTreeViewTagItem> MarkedTags { get; } = new Collection<ViewModel.BaseTreeViewTagItem>();
    public BackgroundWorker ThumbsWorker { get; set; }
    public AutoResetEvent ThumbsResetEvent { get; set; } = new AutoResetEvent(false);
    public ViewModel.Viewer CurrentViewer { get; set; }
    public double WindowsDisplayScale { get; set; }

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
        if (Db != null) {
          Db.Dispose();
          Db = null;
        }
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

    public void Init() {
      WindowsDisplayScale = PresentationSource.FromVisual(WMain)?.CompositionTarget?.TransformToDevice.M11 * 100 ?? 100.0;

      App.SplashScreen.AddMessage("Loading Database");
      Db.Load();
      App.SplashScreen.AddMessage("Loading Viewers");
      Viewers.Load();
      CurrentViewer = Viewers.Items.SingleOrDefault(x => x.Title == Settings.Default.Viewer) as ViewModel.Viewer;
      App.SplashScreen.AddMessage("Loading People");
      People.Load();
      App.SplashScreen.AddMessage("Loading Keywords");
      Keywords.Load();
      App.SplashScreen.AddMessage("Loading Folder Keywords");
      FolderKeywords.Load();
      App.SplashScreen.AddMessage("Loading Folders");
      Folders.AddDrives();
      App.SplashScreen.AddMessage("Loading Favorite Folders");
      FavoriteFolders.Load();
      App.SplashScreen.AddMessage("Loading Ratings");
      Ratings.Load();
      App.SplashScreen.AddMessage("Loading GeoNames");
      GeoNames.Load();
      App.SplashScreen.AddMessage("Loading SqlQueries");
      SqlQueries.Load();
      App.SplashScreen.AddMessage("Loading Media Items");
      MediaItems.LoadAllItems();
      App.SplashScreen.AddMessage("Loading People for Media Items");
      MediaItems.LoadPeople(MediaItems.AllItems);
      App.SplashScreen.AddMessage("Loading Keywords for Media Items");
      MediaItems.LoadKeywords(MediaItems.AllItems);

      FoldersRoot.Add(FavoriteFolders);
      FoldersRoot.Add(Folders);
      KeywordsRoot.Add(Ratings);
      KeywordsRoot.Add(People);
      KeywordsRoot.Add(FolderKeywords);
      KeywordsRoot.Add(Keywords);
      KeywordsRoot.Add(GeoNames);
      FiltersRoot.Add(Viewers);
      FiltersRoot.Add(SqlQueries);
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
      if (item is ViewModel.BaseCategoryItem || item is ViewModel.CategoryGroup) return;

      if (MediaItems.IsEditModeOn) {
        if (!(item is ViewModel.BaseTreeViewTagItem bti)) return;
        switch (item) {
          case ViewModel.Rating _:
          case ViewModel.Person _:
          case ViewModel.Keyword _:
          case ViewModel.GeoName _: {
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
            var folder = Folders.ExpandTo(favoriteFolder.FullPath);
            if (folder != null) {
              var visibleTreeIndex = 0;
              Folders.GetVisibleTreeIndexFor(Folders.Items, folder, ref visibleTreeIndex);
              var offset = (FavoriteFolders.Items.Count + 1 + visibleTreeIndex) * 25;
              WMain.TvFoldersScrollViewer.ScrollToVerticalOffset(offset);
            }
            break;
          }
          case ViewModel.Rating _:
          case ViewModel.Person _:
          case ViewModel.Keyword _:
          case ViewModel.GeoName _: {
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
          case ViewModel.Folder _:
          case ViewModel.FolderKeyword _:
          case ViewModel.SqlQuery _: {
            if (item is ViewModel.Folder folder && !folder.IsAccessible) {
              folder.IsSelected = false;
              return;
            }

            if (bti == null) return;
            bti.IsSelected = true;
            LastSelectedSource = bti;
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
      foreach (var item in MarkedTags) {
        item.IsMarked = false;
        item.PicCount = 0;
      }
      MarkedTags.Clear();

      var mediaItems = MediaItems.GetSelectedOrAll();
      foreach (var mi in mediaItems) {

        foreach (var person in mi.People.Where(person => !person.IsMarked)) {
          person.IsMarked = true;
          MarkedTags.Add(person);
        }

        foreach (var keyword in mi.Keywords) {
          var k = keyword;
          do {
            if (k.IsMarked) break;
            k.IsMarked = true;
            MarkedTags.Add(k);
            k = k.Parent as ViewModel.Keyword;
          } while (k != null);
        }

        if (mi.FolderKeyword != null && !mi.FolderKeyword.IsMarked) {
          var fk = mi.FolderKeyword;
          do {
            if (fk.IsMarked) break;
            fk.IsMarked = true;
            MarkedTags.Add(fk);
            fk = (ViewModel.FolderKeyword) fk.Parent;
          } while (fk != null);
        }

        var geoName = GeoNames.AllGeoNames.SingleOrDefault(x => x.Data.GeoNameId == mi.Data.GeoNameId);
        if (geoName != null && !geoName.IsMarked) {
          var gn = geoName;
          do {
            if (gn.IsMarked) break;
            gn.IsMarked = true;
            MarkedTags.Add(gn);
            gn = (ViewModel.GeoName) gn.Parent;
          } while (gn != null);
        }
      }

      foreach (var rating in mediaItems.Select(p => p.Data.Rating).Distinct().Select(r => Ratings.GetRatingByValue(r))) {
        rating.IsMarked = true;
        MarkedTags.Add(rating);
      }

      foreach (var item in MarkedTags) {
        switch (item) {
          case ViewModel.Person p: {
            p.PicCount = mediaItems.Count(mi => mi.People.Contains(p));
            break;
          }
          case ViewModel.Keyword k: {
            k.PicCount = mediaItems.Count(mi => mi.Keywords.Any(x => x.Data.Name.StartsWith(k.Data.Name)));
            break;
          }
          case ViewModel.FolderKeyword fk: {
            fk.PicCount = mediaItems.Count(mi => mi.FolderKeyword != null && mi.FolderKeyword.FullPath.StartsWith(fk.FullPath));
            break;
          }
          case ViewModel.Rating r: {
            r.PicCount = mediaItems.Count(mi => mi.Data.Rating == r.Value);
            break;
          }
          case ViewModel.GeoName g: {
            //TODO C# how to count files in subdirectories
            var geoNames = new List<ViewModel.GeoName>();
            g.GetThisAndSubGeoNames(ref geoNames);
            g.PicCount = mediaItems.Count(mi => geoNames.Select(gn => (int?) gn.Data.GeoNameId).Contains(mi.Data.GeoNameId));



            /*  var picCount = mediaItems.Count(x => x.GeoNameId == geoName.GeoNameId);
            if (picCount != 0) geoName.PicCount = picCount;
            var parent = geoName.Parent as ViewModel.BaseTreeViewTagItem;
            if (parent != null) parent.PicCount += geoName.PicCount;*/
            break;
          }
        }
      }

      foreach (var pg in People.Items.Where(x => x is ViewModel.CategoryGroup).Cast<ViewModel.CategoryGroup>()) {
        pg.PicCount = pg.Items.Cast<ViewModel.Person>().Sum(x => x.PicCount);
        pg.IsMarked = pg.PicCount > 0;
      }

      foreach (var kg in Keywords.Items.Where(x => x is ViewModel.CategoryGroup).Cast<ViewModel.CategoryGroup>()) {
        kg.PicCount = kg.Items.Cast<ViewModel.Keyword>().Sum(x => x.PicCount);
        kg.IsMarked = kg.PicCount > 0;
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
              mi.SaveMediaItemInToDb(false, (List<DataModel.BaseTable>[]) e.Argument);
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

          if ((bool) Application.Current.Properties[nameof(AppProperty.SubmitChanges)])
            Db.SubmitChanges((List<DataModel.BaseTable>[]) e.Result);

          if (MediaItems.Current != null) {
            MediaItems.Current.IsSelected = false;
            MediaItems.Current.IsSelected = true;
          }

          MarkUsedKeywordsAndPeople();
        }; 
      }

      Application.Current.Properties[nameof(AppProperty.SubmitChanges)] = false;
      ThumbsWorker.RunWorkerAsync(DataModel.PmDataContext.GetInsertUpdateDeleteLists());
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
              case FileOperationMode.Copy: { fo.CopyItem(mi.FilePath, to, mi.Data.FileName); break; }
              case FileOperationMode.Move: { fo.MoveItem(mi.FilePath, to, mi.Data.FileName); break; }
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

      //update DB and thumbnail cache
      using (var fo = new FileOperation()) {
        fo.SetOperationFlags(FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION |
                             FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOFX_KEEPNEWERFILE);
        var cachePath = Settings.Default.CachePath;
        var mItems = Db.MediaItems;
        var dirs = Db.Directories;
        var lists = DataModel.PmDataContext.GetInsertUpdateDeleteLists();

        if (mode == FileOperationMode.Delete) {
          var itemsToDel = new List<DataModel.MediaItem>();

          if (from == null) {
            //delete by file/s
            foreach (var mi in MediaItems.Items.Where(x => x.IsSelected)) {
              if (File.Exists(mi.FilePath)) continue;
              itemsToDel.Add(mi.Data);
              var cacheFilePath = mi.FilePath.Replace(":\\", cachePath);
              if (!File.Exists(cacheFilePath)) continue;
              fo.DeleteItem(cacheFilePath);
            }
          } else {
            //delete by folder
            foreach (var dir in dirs.Where(x => x.Path.Equals(from) || x.Path.StartsWith(from + "\\"))) {
              foreach (var mi in mItems.Where(x => x.DirectoryId.Equals(dir.Id))) {
                var miFilePath = Path.Combine(dir.Path, mi.FileName);
                if (File.Exists(miFilePath)) continue;
                itemsToDel.Add(mi);
                var cacheFilePath = miFilePath.Replace(":\\", cachePath);
                if (!File.Exists(cacheFilePath)) continue;
                fo.DeleteItem(cacheFilePath);
              }
            }
          }

          foreach (var mi in itemsToDel) {
            foreach(var mik in Db.MediaItemKeywords.Where(x => x.MediaItemId == mi.Id)) {
              DataModel.PmDataContext.DeleteOnSubmit(mik, lists);
            }

            foreach (var mip in Db.MediaItemPeople.Where(x => x.MediaItemId == mi.Id)) {
              DataModel.PmDataContext.DeleteOnSubmit(mip, lists);
            }

            DataModel.PmDataContext.DeleteOnSubmit(mi, lists);
          }
        }

        if (mode == FileOperationMode.Copy || mode == FileOperationMode.Move) {
          foreach (var item in foResult) {
            if (MediaItems.SuportedExts.Any(ext => item.Value.EndsWith(ext, StringComparison.OrdinalIgnoreCase))) {
              if (!File.Exists(item.Value)) continue;

              var srcDirId = dirs.SingleOrDefault(x => x.Path.Equals(Path.GetDirectoryName(item.Key)))?.Id;
              if (srcDirId == null) continue;

              var srcPic = mItems.SingleOrDefault(x => x.DirectoryId == srcDirId && x.FileName == Path.GetFileName(item.Key));
              if (srcPic == null) continue;

              //get destination directory or create it if doesn't exists
              var destDirId = Db.InsertDirectoryInToDb(Path.GetDirectoryName(item.Value));

              #region Copy files

              if (mode == FileOperationMode.Copy) {
                //duplicate Picture
                var destPicId = Db.GetNextIdFor<DataModel.MediaItem>();
                var dmiCopy = new DataModel.MediaItem {
                  Id = destPicId,
                  DirectoryId = destDirId,
                  FileName = Path.GetFileName(item.Value),
                  Rating = srcPic.Rating,
                  Comment = srcPic.Comment,
                  Orientation = srcPic.Orientation,
                  GeoNameId = srcPic.GeoNameId,
                  Height = srcPic.Height,
                  Width = srcPic.Width
                };
                DataModel.PmDataContext.InsertOnSubmit(dmiCopy, lists);

                //duplicate Picture Keywords
                foreach (var mik in Db.MediaItemKeywords.Where(x => x.MediaItemId == srcPic.Id)) {
                  DataModel.PmDataContext.InsertOnSubmit(new DataModel.MediaItemKeyword {
                    Id = Db.GetNextIdFor<DataModel.MediaItemKeyword>(),
                    KeywordId = mik.KeywordId,
                    MediaItemId = destPicId
                  }, lists);
                }

                //duplicate Picture People
                foreach (var mip in Db.MediaItemPeople.Where(x => x.MediaItemId == srcPic.Id)) {
                  DataModel.PmDataContext.InsertOnSubmit(new DataModel.MediaItemPerson {
                    Id = Db.GetNextIdFor<DataModel.MediaItemPerson>(),
                    PersonId = mip.PersonId,
                    MediaItemId = destPicId
                  }, lists);
                }

                var miCopy = new ViewModel.BaseMediaItem(item.Value, dmiCopy);
                MediaItems.AllItems.Add(miCopy);
                var list = new List<ViewModel.BaseMediaItem> {miCopy};
                MediaItems.LoadPeople(list);
                MediaItems.LoadKeywords(list);

                //duplicate thumbnail
                fo.CopyItem(item.Key.Replace(":\\", cachePath), Path.GetDirectoryName(item.Value)?.Replace(":\\", cachePath),
                  Path.GetFileName(item.Value));
              }

              #endregion

              #region Move files
              if (mode == FileOperationMode.Move) {
                //BUG: if the file already exists in the destination directory, FileOperation returns COPYENGINE_S_USER_IGNORED and source thumbnail file is not deleted
                srcPic.DirectoryId = destDirId;
                srcPic.FileName = Path.GetFileName(item.Value);
                MediaItems.AllItems.Single(x => x.Data.Id == srcPic.Id).FilePath = item.Value;
                DataModel.PmDataContext.UpdateOnSubmit(srcPic, lists);

                //delete empty directory
                if (mItems.Count(x => x.DirectoryId.Equals(srcDirId)) == 0) {
                  var emptyDir = dirs.SingleOrDefault(x => x.Id.Equals(srcDirId));
                  if (emptyDir != null) {
                    DataModel.PmDataContext.DeleteOnSubmit(emptyDir, lists);
                  }
                }

                //move thumbnail
                fo.MoveItem(item.Key.Replace(":\\", cachePath), Path.GetDirectoryName(item.Value)?.Replace(":\\", cachePath),
                  Path.GetFileName(item.Value));
              }

              #endregion
            } else {
              #region Move directories
              if (mode == FileOperationMode.Move) {
                //test if it is directory
                if (!Directory.Exists(item.Value)) continue;

                foreach (var dir in dirs.Where(x => x.Path.Equals(item.Key) || x.Path.StartsWith(item.Key + "\\"))) {
                  dir.Path = dir.Path.Replace(item.Key, item.Value);
                  DataModel.PmDataContext.UpdateOnSubmit(dir, lists);
                }

                //move thumbnails
                var destPath = Path.GetDirectoryName(item.Value);
                if (destPath != null)
                  fo.MoveItem(item.Key.Replace(":\\", cachePath), destPath.Replace(":\\", cachePath),
                    item.Value.Substring(destPath.EndsWith("\\") ? destPath.Length : destPath.Length + 1));
              }
              #endregion
            }
          }
        }

        fo.PerformOperations();
        Db.SubmitChanges(lists);
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
