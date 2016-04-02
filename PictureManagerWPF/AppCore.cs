﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using PictureManager.Properties;
using PictureManager.ShellStuff;
using Directory = System.IO.Directory;

namespace PictureManager {
  public class AppCore {
    private ViewModel.BaseTreeViewItem _lastSelectedSource;
    public ObservableCollection<ViewModel.BaseTreeViewItem> FoldersRoot;
    public ObservableCollection<ViewModel.BaseTreeViewItem> KeywordsRoot;
    public ObservableCollection<ViewModel.BaseTreeViewItem> FiltersRoot;
    public ViewModel.Keywords Keywords;
    public ViewModel.People People;
    public ViewModel.FolderKeywords FolderKeywords;
    public ViewModel.Folders Folders;
    public ViewModel.FavoriteFolders FavoriteFolders;
    public ViewModel.Ratings Ratings;
    public ViewModel.Filters Filters;
    public ViewModel.Viewers Viewers;

    public WMain WMain;
    public string[] IncorectChars = { "\\", "/", ":", "*", "?", "\"", "<", ">", "|", ";" };
    public System.Windows.Forms.WebBrowser WbThumbs;
    public ViewModel.AppInfo AppInfo;
    public bool OneFileOnly;
    public bool ViewerOnly = false; //application was run with file path parameter
    public enum FileOperations { Copy, Move, Delete }
    public bool LastSelectedSourceRecursive;
    public DataModel.PmDataContext Db;
    public ViewModel.MediaItems MediaItems;
    public List<ViewModel.BaseTreeViewTagItem> MarkedTags;
    public List<ViewModel.BaseTreeViewTagItem> TagModifers;
    public BackgroundWorker ThumbsWebWorker;
    public AutoResetEvent ThumbsResetEvent = new AutoResetEvent(false);
    public int ThumbsPageIndex;
    public int ThumbsPerPage = 300;

    private bool _keywordsEditMode;

    public bool KeywordsEditMode {
      get { return _keywordsEditMode; }
      set {
        _keywordsEditMode = value;
        AppInfo.KeywordsEditMode = value;
      }
    }

    public ViewModel.BaseTreeViewItem LastSelectedSource {
      get { return _lastSelectedSource; }
      set {
        if (_lastSelectedSource == value) return;
        if (_lastSelectedSource != null)
          _lastSelectedSource.IsSelected = false;
        _lastSelectedSource = value;
      }
    }

    public AppCore() {
      AppInfo = new ViewModel.AppInfo();
      MediaItems = new ViewModel.MediaItems(this);
      MarkedTags = new List<ViewModel.BaseTreeViewTagItem>();
      TagModifers = new List<ViewModel.BaseTreeViewTagItem>();

      Db = new DataModel.PmDataContext("Data Source = data.db");
      Db.CreateDbStructure();
      Db.Load();
    }

    public void Init() {
      People = new ViewModel.People {Db = Db};
      Keywords = new ViewModel.Keywords {Db = Db};
      FolderKeywords = new ViewModel.FolderKeywords {Db = Db, ACore = this};
      Folders = new ViewModel.Folders();
      FavoriteFolders = new ViewModel.FavoriteFolders();
      Ratings = new ViewModel.Ratings();
      Filters = new ViewModel.Filters {Db = Db};
      Viewers = new ViewModel.Viewers {Db = Db};

      People.Load();
      People.Load();
      Keywords.Load();
      FolderKeywords.Load();
      Folders.AddDrives();
      FavoriteFolders.Load();
      Ratings.Load();
      Filters.Load();
      Viewers.Load();

      FoldersRoot = new ObservableCollection<ViewModel.BaseTreeViewItem> {FavoriteFolders, Folders};
      KeywordsRoot = new ObservableCollection<ViewModel.BaseTreeViewItem> {Ratings, People, FolderKeywords, Keywords};
      FiltersRoot = new ObservableCollection<ViewModel.BaseTreeViewItem> {Filters, Viewers};
    }

    public void UpdateStatusBarInfo() {
      AppInfo.ViewBaseInfo = $"{MediaItems.Items.Count} object(s) / {MediaItems.Items.Count(x => x.IsSelected)} selected";
      AppInfo.CurrentPictureFilePath = MediaItems.Current == null ? string.Empty : MediaItems.Current.FilePath;
    }

    public void TreeView_KeywordsStackPanel_PreviewMouseUp(object item, MouseButton mouseButton, bool recursive) {
      if (item is ViewModel.Keywords || item is ViewModel.People || item is ViewModel.FolderKeywords || item is ViewModel.Ratings || item is ViewModel.PeopleGroup) return;

      switch (mouseButton) {
        case MouseButton.Left: {
          if (KeywordsEditMode) {
            var fk = item as ViewModel.FolderKeyword;
            if (fk != null) {
              fk.IsSelected = false;
              return;
            }

            var bti = item as ViewModel.BaseTreeViewTagItem;
            if (bti != null) {
                bti.IsMarked = !bti.IsMarked;
              if (bti.IsMarked)
                MarkedTags.Add(bti);
              else
                MarkedTags.Remove(bti);
            }

            MediaItems.EditMetadata(item);

            MarkUsedKeywordsAndPeople();
          } else {
            //not KeywordsEditMode
            var baseTagItem = (ViewModel.BaseTreeViewTagItem) item;
            baseTagItem.IsSelected = true;
            TagModifers.Clear();
            TagModifers.Add(baseTagItem);

            LastSelectedSource = baseTagItem;
            LastSelectedSourceRecursive = recursive;

            if (ThumbsWebWorker != null && ThumbsWebWorker.IsBusy) {
              ThumbsWebWorker.CancelAsync();
              ThumbsResetEvent.WaitOne();
            }

            MediaItems.LoadByTag(baseTagItem, recursive);
            InitThumbsPagesControl();
          }
          break;
        }
        case MouseButton.Middle: {
          //nothing for now
          /*if (KeywordsEditMode) return null;
            if (item.IsCategory || !item.IsAccessible) return null;
            if (!TagModifers.Contains(item))
              TagModifers.Add(item);
            item.IsSelected = !item.IsSelected;
            GetPicturesByTag();
            MarkUsedKeywordsAndPeople();
            CreateThumbnailsWebPage();*/
          break;
        }
      }
    }

    public void MarkUsedKeywordsAndPeople() {
      //can by Person, Keyword, FolderKeyword or Rating
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
            k = k.Parent;
          } while (k != null);
        }

        var folderKeyword = FolderKeywords.GetFolderKeywordByDirId(FolderKeywords.Items, mi.DirId);
        if (folderKeyword != null && !folderKeyword.IsMarked) {
          var fk = folderKeyword;
          do {
            if (fk.IsMarked) break;
            fk.IsMarked = true;
            MarkedTags.Add(fk);
            fk = fk.Parent;
          } while (fk != null);
        }
      }

      foreach (var rating in mediaItems.Select(p => p.Rating).Distinct().Select(r => Ratings.GetRatingByValue(r))) {
        rating.IsMarked = true;
        MarkedTags.Add(rating);
      }

      foreach (var item in MarkedTags) {
        switch (item.GetType().Name) {
          case nameof(ViewModel.Person): {
            var pesron = (ViewModel.Person) item;
            pesron.PicCount = mediaItems.Count(p => p.People.Contains(pesron));
            break;
          }
          case nameof(ViewModel.Keyword): {
            var keyword = (ViewModel.Keyword) item;
            keyword.PicCount = mediaItems.Count(p => p.Keywords.Any(k => k.FullPath.StartsWith(keyword.FullPath)));
            break;
          }
          case nameof(ViewModel.FolderKeyword): {
            var folderKeyword = (ViewModel.FolderKeyword) item;
            folderKeyword.PicCount =
              mediaItems.Count(p => p.FolderKeyword != null && p.FolderKeyword.FullPath.StartsWith(folderKeyword.FullPath));
            break;
          }
          case nameof(ViewModel.Rating): {
            var rating = (ViewModel.Rating) item;
            rating.PicCount = mediaItems.Count(p => p.Rating == rating.Value);
            break;
          }
        }
      }
    }

    public void InitThumbsPagesControl() {
      WMain.CmbThumbPage.Visibility = MediaItems.Items.Count > ThumbsPerPage ? Visibility.Visible : Visibility.Collapsed;
      WMain.CmbThumbPage.Items.Clear();
      var iPageCount = MediaItems.Items.Count / ThumbsPerPage;
      if (MediaItems.Items.Count > iPageCount * ThumbsPerPage) iPageCount++;
      for (int i = 0; i < iPageCount; i++) {
        WMain.CmbThumbPage.Items.Add($"Page {i + 1}");
      }
      WMain.CmbThumbPage.SelectedIndex = 0;
    }

    public void CreateThumbnailsWebPage() {
      var doc = WbThumbs.Document;
      var thumbs = doc?.GetElementById("thumbnails");
      if (thumbs == null) return;

      thumbs.InnerHtml = string.Empty;
      doc.Window?.ScrollTo(0, 0);

      WMain.StatusProgressBar.Value = 0;
      WMain.StatusProgressBar.Maximum = 100;

      ThumbsWebWorker = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};

      ThumbsWebWorker.ProgressChanged += delegate(object sender, ProgressChangedEventArgs e) {
        if (((BackgroundWorker) sender).CancellationPending || e.UserState == null) return;

        var mi = MediaItems.Items[(int) e.UserState];
        var thumb = doc.CreateElement("div");
        var keywords = doc.CreateElement("div");
        var img = doc.CreateElement("img");

        if (thumb == null || keywords == null || img == null) return;

        keywords.SetAttribute("className", "keywords");
        keywords.InnerHtml = mi.GetKeywordsAsString();

        img.SetAttribute("src", mi.FilePathCache);

        thumb.SetAttribute("className", "thumbBox");
        thumb.SetAttribute("id", mi.Index.ToString());
        thumb.AppendChild(keywords);
        thumb.AppendChild(img);
        thumbs.AppendChild(thumb);

        WMain.StatusProgressBar.Value = e.ProgressPercentage;
      };

      ThumbsWebWorker.DoWork += delegate(object sender, DoWorkEventArgs e) {
        var worker = (BackgroundWorker) sender;
        var count = MediaItems.Items.Count;
        var iFrom = ThumbsPageIndex == 0 ? 0 : ThumbsPageIndex * ThumbsPerPage;
        var iTo = count > iFrom + ThumbsPerPage ? iFrom + ThumbsPerPage : count;
        var done = 0;

        for (int i = iFrom; i < iTo; i++) {
          if (worker.CancellationPending) {
            e.Cancel = true;
            ThumbsResetEvent.Set();
            break;
          }
          var mi = MediaItems.Items[i];
          var thumbPath = mi.FilePathCache;
          bool flag = File.Exists(thumbPath);
          if (!flag) CreateThumbnail(mi.FilePath, thumbPath);

          if (mi.Data == null) {
            mi.SaveMediaItemInToDb(this, false, true);
            Application.Current.Properties["SubmitChanges"] = true;
          }

          done++;
          worker.ReportProgress(Convert.ToInt32(((double) done/(iTo - iFrom))*100), mi.Index);
        }
      };

      ThumbsWebWorker.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) {
        if (((BackgroundWorker) sender).CancellationPending) return;
        if ((bool)Application.Current.Properties["SubmitChanges"])
          Db.DataContext.SubmitChanges();
        MediaItems.ScrollToCurrent();
        if (MediaItems.Current != null) {
          MediaItems.Current.IsSelected = false;
          MediaItems.Current.IsSelected = true;
        }
        MarkUsedKeywordsAndPeople();
      };

      Application.Current.Properties["SubmitChanges"] = false;
      ThumbsWebWorker.RunWorkerAsync();
    }

    public bool FileOperation(FileOperations mode, bool recycle) {
      return FileOperation(mode, null, null, null, recycle);
    }

    public bool FileOperation(FileOperations mode, string from, bool recycle) {
      return FileOperation(mode, from, null, null, recycle);
    }

    public bool FileOperation(FileOperations mode, string from, string to, string newName) {
      return FileOperation(mode, from, to, newName, true);
    }

    public bool FileOperation(FileOperations mode, string from, string to, string newName, bool recycle) {
      Application.Current.Properties["FileOperationResult"] = new Dictionary<string, string>();
      //Copy, Move or delete selected MediaItems or folder
      using (FileOperation fo = new FileOperation(new PicFileOperationProgressSink())) {
        var flags = FileOperationFlags.FOF_NOCONFIRMMKDIR | (recycle
          ? FileOperationFlags.FOFX_RECYCLEONDELETE
          : FileOperationFlags.FOF_WANTNUKEWARNING);
        fo.SetOperationFlags(flags);
        if (from == null) { //MediaItems
          foreach (var mi in MediaItems.Items.Where(x => x.IsSelected)) {
            switch (mode) {
              case FileOperations.Copy: { fo.CopyItem(mi.FilePath, to, mi.FileNameWithExt); break; }
              case FileOperations.Move: { fo.MoveItem(mi.FilePath, to, mi.FileNameWithExt); break; }
              case FileOperations.Delete: { fo.DeleteItem(mi.FilePath); break; }
            }
          }
        } else { //Folders
          switch (mode) {
            case FileOperations.Copy: { fo.CopyItem(from, to, newName); break; }
            case FileOperations.Move: { fo.MoveItem(from, to, newName); break; }
            case FileOperations.Delete: { fo.DeleteItem(from); break; }
          }
        }

        fo.PerformOperations();
      }

      var foResult = (Dictionary<string, string>)Application.Current.Properties["FileOperationResult"];
      if (foResult.Count == 0) return false;

      //update DB and thumbnail cache
      using (FileOperation fo = new FileOperation()) {
        fo.SetOperationFlags(FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION |
                             FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOFX_KEEPNEWERFILE);
        var cachePath = @Settings.Default.CachePath;
        var mItems = Db.ListMediaItems;
        var dirs = Db.ListDirectories;

        if (mode == FileOperations.Delete) {
          var itemsToDel = new List<DataModel.MediaItem>();

          if (from == null) {
            //delete by file/s
            foreach (var mi in MediaItems.Items.Where(x => x.IsSelected)) {
              if (File.Exists(mi.FilePath)) continue;
              fo.DeleteItem(mi.FilePath.Replace(":\\", cachePath));
              itemsToDel.Add(mi.Data);
            }
          } else {
            //delete by folder
            foreach (var dir in dirs.Where(x => x.Path.Equals(from) || x.Path.StartsWith(from + "\\"))) {
              foreach (var mi in mItems.Where(x => x.DirectoryId.Equals(dir.Id))) {
                var miFilePath = Path.Combine(dir.Path, mi.FileName);
                if (File.Exists(miFilePath)) continue;
                fo.DeleteItem(miFilePath.Replace(":\\", cachePath));
                itemsToDel.Add(mi);
              }
            }
          }

          foreach (var mi in itemsToDel) {
            foreach(var mik in Db.ListMediaItemKeywords.Where(x => x.MediaItemId == mi.Id)) {
              Db.DeleteOnSubmit(mik);
            }

            foreach (var mip in Db.ListMediaItemPeople.Where(x => x.MediaItemId == mi.Id)) {
              Db.DeleteOnSubmit(mip);
            }

            Db.DeleteOnSubmit(mi);
          }
        }

        if (mode == FileOperations.Copy || mode == FileOperations.Move) {
          foreach (var item in foResult) {
            if (MediaItems.SuportedExts.Any(ext => item.Value.EndsWith(ext, StringComparison.OrdinalIgnoreCase))) {
              if (!File.Exists(item.Value)) continue;

              var srcDirId = dirs.SingleOrDefault(x => x.Path.Equals(Path.GetDirectoryName(item.Key)))?.Id;
              if (srcDirId == null) continue;

              var srcPic = mItems.SingleOrDefault(x => x.DirectoryId == srcDirId && x.FileName == Path.GetFileName(item.Key));
              if (srcPic == null) continue;

              //get destination directory or create it if doesn't exists
              var dirPath = Path.GetDirectoryName(item.Value);
              var destDirId = dirs.SingleOrDefault(x => x.Path.Equals(dirPath))?.Id;
              if (destDirId == null) {
                destDirId = Db.GetNextIdFor("Directories");
                Db.InsertOnSubmit(new DataModel.Directory { Id = (long)destDirId, Path = dirPath });
              }

              #region Copy files

              if (mode == FileOperations.Copy) {
                //duplicate Picture
                var destPicId = Db.GetNextIdFor("MediaItems");

                Db.InsertOnSubmit(new DataModel.MediaItem {
                  Id = destPicId,
                  DirectoryId = (long) destDirId,
                  FileName = Path.GetFileName(item.Value),
                  Rating = srcPic.Rating,
                  Comment = srcPic.Comment,
                  Orientation = srcPic.Orientation
                });

                //duplicate Picture Keywords
                foreach (var mik in Db.ListMediaItemKeywords.Where(x => x.MediaItemId == srcPic.Id)) {
                  Db.InsertOnSubmit(new DataModel.MediaItemKeyword {
                    Id = Db.GetNextIdFor("MediaItemKeyword"),
                    KeywordId = mik.KeywordId,
                    MediaItemId = destPicId
                  });
                }

                //duplicate Picture People
                foreach (var mip in Db.ListMediaItemPeople.Where(x => x.MediaItemId == srcPic.Id)) {
                  Db.InsertOnSubmit(new DataModel.MediaItemPerson {
                    Id = Db.GetNextIdFor("MediaItemPerson"),
                    PersonId = mip.PersonId,
                    MediaItemId = destPicId
                  });
                }

                //duplicate thumbnail
                fo.CopyItem(item.Key.Replace(":\\", cachePath), Path.GetDirectoryName(item.Value)?.Replace(":\\", cachePath),
                  Path.GetFileName(item.Value));
              }

              #endregion

              #region Move files
              if (mode == FileOperations.Move) {
                //BUG: if the file already exists in the destination directory, FileOperation returns COPYENGINE_S_USER_IGNORED and source thumbnail file is not deleted
                srcPic.DirectoryId = (long) destDirId;
                srcPic.FileName = Path.GetFileName(item.Value);

                //delete empty directory
                if (mItems.Count(x => x.DirectoryId.Equals(srcDirId)) == 0) {
                  var emptyDir = dirs.SingleOrDefault(x => x.Id.Equals(srcDirId));
                  if (emptyDir != null) {
                    Db.DeleteOnSubmit(emptyDir);
                  }
                }

                //move thumbnail
                fo.MoveItem(item.Key.Replace(":\\", cachePath), Path.GetDirectoryName(item.Value)?.Replace(":\\", cachePath),
                  Path.GetFileName(item.Value));
              }

              #endregion
            } else {
              #region Move directories
              if (mode == FileOperations.Move) {
                //test if it is directory
                if (!Directory.Exists(item.Value)) continue;

                foreach (var dir in dirs.Where(x => x.Path.Equals(item.Key) || x.Path.StartsWith(item.Key + "\\"))) {
                  dir.Path = dir.Path.Replace(item.Key, item.Value);
                }

                //move thumbnails
                var destPath = Path.GetDirectoryName(item.Value);
                fo.MoveItem(item.Key.Replace(":\\", cachePath), destPath.Replace(":\\", cachePath),
                  item.Value.Substring(destPath.Length + 1));
              }
              #endregion
            }
          }
        }

        fo.PerformOperations();
        Db.DataContext.SubmitChanges();
      }

      return true;
    }

    public long? GetDirectoryIdByPath(string path) {
      var dir = Db.ListDirectories.SingleOrDefault(x => x.Path.Equals(path));
      return dir?.Id;
    }

    public long InsertDirecotryInToDb(string path) {
      var dirId = GetDirectoryIdByPath(path);
      if (dirId != null) return (long) dirId;
      var newDirId = Db.GetNextIdFor("Directories");
      //Db.Directories.InsertOnSubmit(new DataModel.Directory {Id = newDirId, Path = path});
      Db.InsertOnSubmit(new DataModel.Directory { Id = newDirId, Path = path });
      Db.DataContext.SubmitChanges();
      return newDirId;
    }

    public bool CanViewerSeeThisFile(string filePath) {
      var ok = false;
      var viewer = Viewers.Items.SingleOrDefault(x => x.Title == Settings.Default.Viewer);
      if (viewer != null) {
        if (viewer.DirsAllowed.Any(x => filePath.StartsWith(x, StringComparison.OrdinalIgnoreCase))) {
          if (viewer.DirsDenied.Any(x => filePath.StartsWith(x, StringComparison.OrdinalIgnoreCase))) {
            ok = viewer.FilesAllowed.Any(x => filePath.Equals(x, StringComparison.OrdinalIgnoreCase));
          } else {
            ok = !viewer.FilesDenied.Any(x => filePath.Equals(x, StringComparison.OrdinalIgnoreCase));
          }
        } else {
          ok = viewer.FilesAllowed.Any(x => filePath.Equals(x, StringComparison.OrdinalIgnoreCase));
        }
      }
      return ok;
    }

    public bool CanViewerSeeThisDirectory(string dirPath) {
      var ok = false;
      var viewer = Viewers.Items.SingleOrDefault(x => x.Title == Settings.Default.Viewer);
      if (viewer != null) {
        if (viewer.DirsAllowed.Any(x => x.Contains(dirPath)) || viewer.DirsAllowed.Any(dirPath.Contains)) {
          if (viewer.DirsDenied.Any(x => x.Contains(dirPath)) || viewer.DirsDenied.Any(dirPath.Contains)) {
            ok = viewer.FilesAllowed.Any(x => x.StartsWith(dirPath));
          } else {
            ok = !viewer.FilesDenied.Any(x => x.StartsWith(dirPath));
          }
        } else {
          ok = viewer.FilesAllowed.Any(x => x.StartsWith(dirPath));
        }
      }
      return ok;
    }

    public static void CreateThumbnail(string origPath, string newPath) {
      int size = Settings.Default.ThumbnailSize;
      string dir = Path.GetDirectoryName(newPath);
      if (dir == null) return;
      Directory.CreateDirectory(dir);

      var process = new Process {
        StartInfo = new ProcessStartInfo {
          Arguments = $"src|\"{origPath}\" dest|\"{newPath}\" quality|\"{80}\" size|\"{size}\"",
          FileName = "ThumbnailCreator.exe",
          UseShellExecute = false,
          CreateNoWindow = true
        }
      };

      process.Start();
      process.WaitForExit(1000);
    }
  }
}
