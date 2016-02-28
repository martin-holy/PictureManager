using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using PictureManager.Data;
using PictureManager.Properties;
using PictureManager.ShellStuff;

namespace PictureManager {
  public class AppCore {
    private BaseItem _lastSelectedSource;
    public ObservableCollection<BaseItem> FoldersRoot;
    public ObservableCollection<BaseItem> KeywordsRoot;
    public Keywords Keywords;
    public People People;
    public FolderKeywords FolderKeywords;
    public Folders Folders;
    public FavoriteFolders FavoriteFolders;
    public Ratings Ratings;
    public WMain WMain;
    public string[] IncorectChars = { "\\", "/", ":", "*", "?", "\"", "<", ">", "|", ";" };
    public System.Windows.Forms.WebBrowser WbThumbs;
    public AppInfo AppInfo;
    public bool OneFileOnly;
    public bool ViewerOnly = false; //application was run with file path parameter
    public enum FileOperations { Copy, Move, Delete }
    public bool LastSelectedSourceRecursive;
    public DbStuff Db;
    public MediaItems MediaItems;
    public List<BaseTagItem> MarkedTags;
    public List<BaseTagItem> TagModifers;
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

    public BaseItem LastSelectedSource {
      get { return _lastSelectedSource; }
      set {
        if (_lastSelectedSource == value) return;
        if (_lastSelectedSource != null)
          _lastSelectedSource.IsSelected = false;
        _lastSelectedSource = value;
      }
    }

    public AppCore() {
      AppInfo = new AppInfo();
      MediaItems = new MediaItems(this);
      MarkedTags = new List<BaseTagItem>();
      TagModifers = new List<BaseTagItem>();
      
      Db = new DbStuff("Data Source = data.db");
      Db.CreateDbStructure();
    }

    public void Init() {
      People = new People {Db = Db, Title = "People", IconName = "appbar_people_multiple"};
      Keywords = new Keywords {Db = Db, Title = "Keywords", IconName = "appbar_tag"};
      FolderKeywords = new FolderKeywords {Db = Db, Title = "Folder Keywords", IconName = "appbar_folder" };
      Folders = new Folders {Title = "Folders", IconName = "appbar_folder"};
      FavoriteFolders = new FavoriteFolders { Title = "Favorites", IconName = "appbar_folder_star"};
      Ratings = new Ratings { Title = "Ratings", IconName = "appbar_star" };

      People.Load();
      Keywords.Load();
      FolderKeywords.Load();
      Folders.AddDrives();
      FavoriteFolders.Load();
      Ratings.Load();

      KeywordsRoot = new ObservableCollection<BaseItem> {Ratings, People, FolderKeywords, Keywords};
      FoldersRoot = new ObservableCollection<BaseItem> {FavoriteFolders, Folders};
    }

    public void UpdateStatusBarInfo() {
      AppInfo.ViewBaseInfo = $"{MediaItems.Items.Count} object(s) / {MediaItems.Items.Count(x => x.IsSelected)} selected";
      AppInfo.CurrentPictureFilePath = MediaItems.Current == null ? string.Empty : MediaItems.Current.FilePath;
    }

    public void TreeView_KeywordsStackPanel_PreviewMouseUp(object item, MouseButton mouseButton, bool recursive) {
      if (item is Keywords || item is People || item is FolderKeywords || item is Ratings) return;

      switch (mouseButton) {
        case MouseButton.Left: {
          if (KeywordsEditMode) {
            var fk = item as FolderKeyword;
            if (fk != null) {
              fk.IsSelected = false;
              return;
            }

            var baseTagItem = item as BaseTagItem;
            if (baseTagItem != null) {
              baseTagItem.IsMarked = !baseTagItem.IsMarked;
              if (baseTagItem.IsMarked)
                MarkedTags.Add(baseTagItem);
              else
                MarkedTags.Remove(baseTagItem);
            }

            MediaItems.EditMetadata(item);

            MarkUsedKeywordsAndPeople();
          } else {
            //not KeywordsEditMode
            var baseTagItem = (BaseTagItem) item;
            baseTagItem.IsSelected = true;
            TagModifers.Clear();
            TagModifers.Add(baseTagItem);

            LastSelectedSource = baseTagItem;
            LastSelectedSourceRecursive = recursive;

            if (ThumbsWebWorker != null && ThumbsWebWorker.IsBusy) {
              ThumbsWebWorker.CancelAsync();
              ThumbsResetEvent.WaitOne();
            }

            MediaItems.LoadByTag();
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
      foreach (BaseTagItem item in MarkedTags) {
        item.IsMarked = false;
        item.PicCount = 0;
      }
      MarkedTags.Clear();

      var mediaItems = MediaItems.GetSelectedOrAll();
      foreach (BaseMediaItem mi in mediaItems) {

        foreach (Person person in mi.People.Where(person => !person.IsMarked)) {
          person.IsMarked = true;
          MarkedTags.Add(person);
        }

        foreach (Keyword keyword in mi.Keywords) {
          var k = keyword;
          do {
            if (k.IsMarked) break;
            k.IsMarked = true;
            MarkedTags.Add(k);
            k = k.Parent;
          } while (k != null);
        }

        FolderKeyword folderKeyword = FolderKeywords.GetFolderKeywordByDirId(FolderKeywords.Items, mi.DirId);
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
          case nameof(Person): {
            ((Person) item).PicCount = mediaItems.Count(p => p.People.Contains((Person) item));
            break;
          }
          case nameof(Keyword): {
            Keyword keyword = (Keyword) item;
            keyword.PicCount = mediaItems.Count(p => p.Keywords.Any(k => k.FullPath.StartsWith(keyword.FullPath)));
            break;
          }
          case nameof(FolderKeyword): {
            FolderKeyword folderKeyword = (FolderKeyword) item;
            folderKeyword.PicCount =
              mediaItems.Count(p => p.FolderKeyword != null && p.FolderKeyword.FullPath.StartsWith(folderKeyword.FullPath));
            break;
          }
          case nameof(Rating): {
            Rating rating = (Rating) item;
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

        //foreach (var mi in MediaItems.Items) {
        for (int i = iFrom; i < iTo; i++) {
          var mi = MediaItems.Items[i];
          if (worker.CancellationPending) {
            e.Cancel = true;
            ThumbsResetEvent.Set();
            break;
          }

          var thumbPath = mi.FilePathCache;
          bool flag = File.Exists(thumbPath);
          if (!flag) CreateThumbnail(mi.FilePath, thumbPath);

          if (mi.Id == -1) {
            mi.LoadFromDb(this);
          }

          done++;
          worker.ReportProgress(Convert.ToInt32(((double) done/(iTo - iFrom))*100), mi.Index);
        }
      };

      ThumbsWebWorker.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) {
        if (((BackgroundWorker) sender).CancellationPending) return;
        MediaItems.ScrollToCurrent();
        if (MediaItems.Current != null) {
          MediaItems.Current.IsSelected = false;
          MediaItems.Current.IsSelected = true;
        }
        MarkUsedKeywordsAndPeople();
      };

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

      var fileOperationResult = (Dictionary<string, string>) Application.Current.Properties["FileOperationResult"];
      if (fileOperationResult.Count == 0) return false;

      //update DB and thumbnail cache
      using (FileOperation fo = new FileOperation()) {
        fo.SetOperationFlags(FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION |
                             FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOFX_KEEPNEWERFILE);
        var cachePath = @Settings.Default.CachePath;

        foreach (
          var item in
            fileOperationResult.Where(x => MediaItems.SuportedExts.Any(ext => x.Key.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))) {
          int? srcDirId = Db.GetDirectoryIdByPath(Path.GetDirectoryName(item.Key));
          if (srcDirId == null) continue;
          int? srcPicId =
            (int?)
              (long?)
                Db.ExecuteScalar(
                  $"select Id from MediaItems where DirectoryId={srcDirId} and FileName=\"{Path.GetFileName(item.Key)}\"");
          if (srcPicId == null) continue;
          int? destDirId = item.Value == null ? null : Db.InsertDirecotryInToDb(Path.GetDirectoryName(item.Value));
          if (destDirId == null && mode != FileOperations.Delete) continue;

          switch (mode) {
            case FileOperations.Copy: {
              //duplicate Picture
              Db.Execute(
                $"insert into MediaItems (DirectoryId, FileName, Rating, Comment) select {destDirId}, \"{Path.GetFileName(item.Value)}\", Rating, Comment from MediaItems where Id={srcPicId}");
              int? destPicId = Db.GetLastIdFor("MediaItems");
              if (destPicId == null) continue;
              //duplicate Picture Keywords
              Db.Execute(
                $"insert into MediaItemKeyword (MediaItemId, KeywordId) select {destPicId}, KeywordId from MediaItemKeyword where MediaItemId={srcPicId}");
              //duplicate Picture People
              Db.Execute(
                $"insert into MediaItemPerson (MediaItemId, PersonId) select {destPicId}, PersonId from MediaItemPerson where MediaItemId={srcPicId}");
              //duplicate thumbnail
              fo.CopyItem(item.Key.Replace(":\\", cachePath), Path.GetDirectoryName(item.Value)?.Replace(":\\", cachePath),
                Path.GetFileName(item.Value));
              break;
            }
            case FileOperations.Move: {
              //BUG: if the file already exists in the destination directory, FileOperation returns COPYENGINE_S_USER_IGNORED and source thumbnail file is not deleted
              Db.Execute(
                $"update MediaItems set DirectoryId={destDirId}, FileName=\"{Path.GetFileName(item.Value)}\" where Id={srcPicId}");
              fo.MoveItem(item.Key.Replace(":\\", cachePath), Path.GetDirectoryName(item.Value)?.Replace(":\\", cachePath),
                Path.GetFileName(item.Value));
              break;
            }
            case FileOperations.Delete: {
              Db.Execute($"delete from MediaItemKeyword where MediaItemId={srcPicId}");
              Db.Execute($"delete from MediaItemPerson where MediaItemId={srcPicId}");
              Db.Execute($"delete from MediaItems where Id={srcPicId}");
              fo.DeleteItem(item.Key.Replace(":\\", cachePath));
              break;
            }
          }
        }

        //move, rename or delete folder in a TreeView
        if (from != null) {
          switch (mode) {
            case FileOperations.Move: {
              var newFullPath = fileOperationResult[from];
              var newName2 = newFullPath.Substring(newFullPath.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase) + 1);
              fo.MoveItem(from.Replace(":\\", cachePath), to.Replace(":\\", cachePath), newName2);

              string sql = $"select Id, Path from Directories where Path like '{from}%'";
              foreach (DataRow dir in Db.Select(sql)) {
                Db.Execute($"update Directories set Path='{((string) dir[1]).Replace(from, newFullPath)}' where Id={dir[0]}");
              }
              break;
            }
            case FileOperations.Delete: {
              fo.DeleteItem(from.Replace(":\\", cachePath));
              break;
            }
          }
        }

        fo.PerformOperations();
      }

      return true;
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
