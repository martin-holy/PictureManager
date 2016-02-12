using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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
    public string[] SuportedExts = { ".jpg", ".jpeg" };

    public BaseItem LastSelectedSource {
      get {
        return _lastSelectedSource;
      }
      set {
        if (_lastSelectedSource == value) return;
        if (_lastSelectedSource != null)
          _lastSelectedSource.IsSelected = false;
        _lastSelectedSource = value;
      }
    }

    public bool LastSelectedSourceRecursive;

    public DbStuff Db;
    public ObservableCollection<Picture> Pictures;
    public ObservableCollection<Picture> SelectedPictures;
    public List<BaseTagItem> MarkedTags;
    public List<BaseTagItem> TagModifers; 

    private Picture _currentPicture;
    public Picture CurrentPicture {
      get {
        return _currentPicture;
      }
      set {
        _currentPicture = value;
        AppInfo.CurrentPictureFilePath = value == null ? string.Empty : value.FilePath;
      }
    }

    public System.Windows.Forms.WebBrowser WbThumbs;
    public AppInfo AppInfo;
    public bool OneFileOnly;
    public bool ViewerOnly = false; //application was run with file path parameter
    public bool KeywordsEditMode = false;
    public enum FileOperations {Copy, Move, Delete}
    
    public AppCore() {
      AppInfo = new AppInfo();
      Pictures = new ObservableCollection<Picture>();
      SelectedPictures = new ObservableCollection<Picture>();
      MarkedTags = new List<BaseTagItem>();
      TagModifers = new List<BaseTagItem>();
      
      
      Db = new DbStuff("Data Source = data.db");
      Db.CreateDbStructure();

      Pictures.CollectionChanged += delegate(object sender, NotifyCollectionChangedEventArgs args) {
        if (args.Action == NotifyCollectionChangedAction.Reset)
          SelectedPictures.Clear();
        UpdateViewBaseInfo();
      };
      SelectedPictures.CollectionChanged += delegate { UpdateViewBaseInfo(); };
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

    public void UpdateViewBaseInfo() {
      AppInfo.ViewBaseInfo = $"{Pictures.Count} object(s) / {SelectedPictures.Count} selected";
    }

    public void TreeView_KeywordsStackPanel_PreviewMouseUp(object item, MouseButton mouseButton, bool recursive) {
      if (item is Keywords || item is People || item is FolderKeywords || item is Ratings) return;

      switch (mouseButton) {
        case MouseButton.Left: {
          if (KeywordsEditMode) {
            if (item is FolderKeyword) {
              ((FolderKeyword) item).IsSelected = false;
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

            foreach (Picture picture in SelectedPictures) {
              picture.IsModifed = true;

              switch (item.GetType().Name) {
                case nameof(Person): {
                  var person = (Person) item;
                  if (person.IsMarked) {
                    picture.People.Add(person);
                  } else {
                    picture.People.Remove(person);
                  }
                  break;
                }
                case nameof(Keyword): {
                  var keyword = (Keyword) item;
                  if (keyword.IsMarked) {
                    //remove potencial redundant keywords (example: if new keyword is "#CoSpi/Sunny" keyword "#CoSpi" is redundant)
                    for (int i = picture.Keywords.Count - 1; i >= 0; i--) {
                      if (keyword.FullPath.StartsWith(picture.Keywords[i].FullPath)) {
                        picture.Keywords.RemoveAt(i);
                      }
                    }
                    picture.Keywords.Add(keyword);
                  } else {
                    picture.Keywords.Remove(keyword);
                  }
                  break;
                }
                case nameof(Rating): {
                  var rating = (Rating) item;
                  picture.Rating = rating.Value;
                  break;
                }
              }
              WbUpdatePictureInfo(picture.Index);
            }
            MarkUsedKeywordsAndPeople();
          } else {
            //not KeywordsEditMode
            var baseTagItem = (BaseTagItem) item;
            baseTagItem.IsSelected = true;
            TagModifers.Clear();
            TagModifers.Add(baseTagItem);

            LastSelectedSource = baseTagItem;
            LastSelectedSourceRecursive = recursive;
            GetPicturesByTag();
            MarkUsedKeywordsAndPeople();
            CreateThumbnailsWebPage();
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

    public bool CurrentPictureMove(bool next) {
      LoadOtherPictures();
      if (next) {
        if (CurrentPicture.Index < Pictures.Count - 1) {
          CurrentPicture = Pictures[CurrentPicture.Index + 1];
          return true;
        }
      } else {
        if (CurrentPicture.Index > 0) {
          CurrentPicture = Pictures[CurrentPicture.Index - 1];
          return true;
        }
      }
      return false;
    }

    public void MarkUsedKeywordsAndPeople() {
      //can by Person, Keyword, FolderKeyword or Rating
      foreach (BaseTagItem item in MarkedTags) {
        item.IsMarked = false;
        item.PicCount = 0;
      }

      MarkedTags.Clear();
      var pictures = SelectedPictures.Count == 0 ? Pictures : SelectedPictures;
      foreach (Picture picture in pictures) {

        foreach (Person person in picture.People.Where(person => !person.IsMarked)) {
          person.IsMarked = true;
          MarkedTags.Add(person);
        }

        foreach (Keyword keyword in picture.Keywords) {
          var k = keyword;
          do {
            if (k.IsMarked) break;
            k.IsMarked = true;
            MarkedTags.Add(k);
            k = k.Parent;
          } while (k != null);
        }

        FolderKeyword folderKeyword = FolderKeywords.GetFolderKeywordByDirId(FolderKeywords.Items, picture.DirId);
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

      foreach (var rating in pictures.Select(p => p.Rating).Distinct().Select(r => Ratings.GetRatingByValue(r))) {
        rating.IsMarked = true;
        MarkedTags.Add(rating);
      }

      foreach (var item in MarkedTags) {
        switch (item.GetType().Name) {
          case nameof(Person): {
            ((Person) item).PicCount = pictures.Count(p => p.People.Contains((Person) item));
            break;
          }
          case nameof(Keyword): {
            Keyword keyword = (Keyword) item;
            keyword.PicCount = pictures.Count(p => p.Keywords.Any(k => k.FullPath.StartsWith(keyword.FullPath)));
            break;
          }
          case nameof(FolderKeyword): {
            FolderKeyword folderKeyword = (FolderKeyword) item;
            folderKeyword.PicCount =
              pictures.Count(p => p.FolderKeyword != null && p.FolderKeyword.FullPath.StartsWith(folderKeyword.FullPath));
            break;
          }
          case nameof(Rating): {
            Rating rating = (Rating) item;
            rating.PicCount = pictures.Count(p => p.Rating == rating.Value);
            break;
          }
        }
      }
    }

    public void GetPicturesByFolder(string path) {
      if (!Directory.Exists(path)) return;
      Pictures.Clear();

      foreach (
        string file in
          Directory.EnumerateFiles(path)
            .Where(f => SuportedExts.Any(x => f.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x)) {
        Pictures.Add(new Picture(file.Replace(":\\\\", ":\\"), Db, Pictures.Count));
      }
      CurrentPicture = Pictures.Count == 0 ? null : Pictures[0];
    }

    public void GetPicturesByTag() {
      Pictures.Clear();

      var peopleIn = string.Join(",", TagModifers.Where(x => x.IsSelected && x is Person).Cast<Person>().Select(x => x.Id));
      var peopleOut = string.Join(",", TagModifers.Where(x => !x.IsSelected && x is Person).Cast<Person>().Select(x => x.Id));
      var keywordsIn = string.Join(",", TagModifers.Where(x => x.IsSelected && x is Keyword).Cast<Keyword>().Select(x => x.Id));
      var keywordsOut = string.Join(",", TagModifers.Where(x => !x.IsSelected && x is Keyword).Cast<Keyword>().Select(x => x.Id));

      List<string> sqlList = new List<string>();

      if (!string.IsNullOrEmpty(peopleIn))
        sqlList.Add($"select PictureId from PicturePerson where PersonId in ({peopleIn})");
      if (!string.IsNullOrEmpty(peopleOut))
        sqlList.Add($"select PictureId from PicturePerson where PictureId not in (select PictureId from PicturePerson where PersonId in ({peopleOut}))");
      if (!string.IsNullOrEmpty(keywordsIn))
        sqlList.Add($"select PictureId from PictureKeyword where KeywordId in ({keywordsIn})");
      if (!string.IsNullOrEmpty(keywordsOut))
        sqlList.Add($"select PictureId from PictureKeyword where KeywordId not in (select PictureId from PictureKeyword where KeywordId in ({keywordsOut}))");
      if (LastSelectedSourceRecursive && LastSelectedSource is Keyword)
        sqlList.Add($"select PictureId from PictureKeyword where KeywordId in (select Id from Keywords where Keyword like \"{((Keyword)LastSelectedSource).FullPath}%\")");
      var folderKeyword = LastSelectedSource as FolderKeyword;
      if (!string.IsNullOrEmpty(folderKeyword?.FolderIds))
        sqlList.Add($"select Id from Pictures where DirectoryId in ({folderKeyword.FolderIds})");

      string innerSql = string.Join(" union ", sqlList);
      string sql = "select Id, (select Path from Directories as D where D.Id = P.DirectoryId) as Path, FileName, Rating, DirectoryId " +
                   $"from Pictures as P where P.Id in ({innerSql}) order by FileName";

      foreach (DataRow row in Db.Select(sql)) {
        string picFullPath = Path.Combine((string)row[1], (string)row[2]);
        if (File.Exists(picFullPath)) {
          Picture pic = new Picture(picFullPath, Db, Pictures.Count) {
            Id = (int) (long) row[0],
            Rating = (int) (long) row[3],
            DirId = (int) (long) row[4],
            FolderKeyword = FolderKeywords.GetFolderKeywordByFullPath((string)row[1])
          };
          pic.LoadKeywordsFromDb(Keywords);
          pic.LoadPeopleFromDb(People);
          Pictures.Add(pic);
        }
      }
      CurrentPicture = Pictures.Count == 0 ? null : Pictures[0];
    }

    public void CreateThumbnailsWebPage() {
      var doc = WbThumbs.Document;
      var thumbs = doc?.GetElementById("thumbnails");
      if (thumbs == null) return;

      thumbs.InnerHtml = string.Empty;
      DoEvents();

      WMain.StatusProgressBar.Value = 0;
      WMain.StatusProgressBar.Maximum = Pictures.Count;

      foreach (var picture in Pictures) {
        string thumbPath = picture.CacheFilePath;
        bool flag = File.Exists(thumbPath);
        if (!flag) CreateThumbnail(picture.FilePath, thumbPath);

        var thumb = doc.CreateElement("div");
        var keywords = doc.CreateElement("div");
        var img = doc.CreateElement("img");

        if (thumb == null || keywords == null || img == null) continue;

        keywords.SetAttribute("className", "keywords");
        keywords.InnerHtml = picture.GetKeywordsAsString();

        img.SetAttribute("src", thumbPath);

        thumb.SetAttribute("className", "thumbBox");
        thumb.SetAttribute("id", picture.Index.ToString());
        thumb.AppendChild(keywords);
        thumb.AppendChild(img);
        thumbs.AppendChild(thumb);

        if (!flag) DoEvents();
        WMain.StatusProgressBar.Value++;
      }
      
      WMain.StatusProgressBar.Value = 0;
    }

    public void ScrollToCurrent() {
      var doc = WbThumbs.Document;
      if (doc == null) return;
      if (CurrentPicture == null || CurrentPicture.Index == 0) return;
      var thumb = doc.GetElementById(CurrentPicture.Index.ToString());
      thumb?.ScrollIntoView(true);
    }

    public void InitPictures(string dir) {
      int? dirId = Db.InsertDirecotryInToDb(dir);
      if (dirId == null) return;

      FolderKeywords.Load();
      FolderKeyword fk = FolderKeywords.GetFolderKeywordByFullPath(dir);
      WMain.StatusProgressBar.Value = 0;
      WMain.StatusProgressBar.Maximum = Pictures.Count;
      for (int i = 0; i < Pictures.Count; i++) {
        Pictures[i].DirId = (int) dirId;
        Pictures[i].FolderKeyword = fk;
        Pictures[i].LoadFromDb(Keywords, People);
        WbUpdatePictureInfo(i);
        WMain.StatusProgressBar.Value++;
        //DoEvents();
      }
      WMain.StatusProgressBar.Value = 0;
    }

    public void LoadOtherPictures() {
      if (OneFileOnly) {
        string filePath = Pictures[0].FilePath;
        GetPicturesByFolder(Path.GetDirectoryName(filePath));
        CurrentPicture = Pictures.FirstOrDefault(p => p.FilePath == filePath);
        OneFileOnly = false;
      }
    }

    public void WbUpdatePictureInfo(int index) {
      var doc = WbThumbs.Document;
      var thumb = doc?.GetElementById(index.ToString());
      if (thumb == null) return;
      foreach (System.Windows.Forms.HtmlElement element in thumb.Children) {
        if (!element.GetAttribute("className").Equals("keywords")) continue;
        element.InnerHtml = Pictures[index].GetKeywordsAsString();
        break;
      }
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
      //Copy, Move or delete selected pictures or folder
      using (FileOperation fo = new FileOperation(new PicFileOperationProgressSink())) {
        var flags = FileOperationFlags.FOF_NOCONFIRMMKDIR | (recycle
          ? FileOperationFlags.FOFX_RECYCLEONDELETE
          : FileOperationFlags.FOF_WANTNUKEWARNING);
        fo.SetOperationFlags(flags);
        if (from == null) { //Pictures
          foreach (var p in SelectedPictures) {
            switch (mode) {
              case FileOperations.Copy: { fo.CopyItem(p.FilePath, to, p.FileName); break; }
              case FileOperations.Move: { fo.MoveItem(p.FilePath, to, p.FileName); break; }
              case FileOperations.Delete: { fo.DeleteItem(p.FilePath); break; }
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
            fileOperationResult.Where(x => SuportedExts.Any(ext => x.Key.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))) {
          int? srcDirId = Db.GetDirectoryIdByPath(Path.GetDirectoryName(item.Key));
          if (srcDirId == null) continue;
          int? srcPicId =
            (int?)
              (long?)
                Db.ExecuteScalar(
                  $"select Id from Pictures where DirectoryId={srcDirId} and FileName=\"{Path.GetFileName(item.Key)}\"");
          if (srcPicId == null) continue;
          int? destDirId = item.Value == null ? null : Db.InsertDirecotryInToDb(Path.GetDirectoryName(item.Value));
          if (destDirId == null && mode != FileOperations.Delete) continue;

          switch (mode) {
            case FileOperations.Copy: {
              //duplicate Picture
              Db.Execute(
                $"insert into Pictures (DirectoryId, FileName, Rating) select {destDirId}, \"{Path.GetFileName(item.Value)}\", Rating from Pictures where Id={srcPicId}");
              int? destPicId = Db.GetLastIdFor("Pictures");
              if (destPicId == null) continue;
              //duplicate Picture Keywords
              Db.Execute(
                $"insert into PictureKeyword (PictureId, KeywordId) select {destPicId}, KeywordId from PictureKeyword where PictureId={srcPicId}");
              //duplicate Picture People
              Db.Execute(
                $"insert into PicturePerson (PictureId, PersonId) select {destPicId}, PersonId from PicturePerson where PictureId={srcPicId}");
              //duplicate thumbnail
              fo.CopyItem(item.Key.Replace(":\\", cachePath), Path.GetDirectoryName(item.Value)?.Replace(":\\", cachePath),
                Path.GetFileName(item.Value));
              break;
            }
            case FileOperations.Move: {
              //BUG: if the file already exists in the destination directory, FileOperation returns COPYENGINE_S_USER_IGNORED and source thumbnail file is not deleted
              Db.Execute(
                $"update Pictures set DirectoryId={destDirId}, FileName=\"{Path.GetFileName(item.Value)}\" where Id={srcPicId}");
              fo.MoveItem(item.Key.Replace(":\\", cachePath), Path.GetDirectoryName(item.Value)?.Replace(":\\", cachePath),
                Path.GetFileName(item.Value));
              break;
            }
            case FileOperations.Delete: {
              Db.Execute($"delete from PictureKeyword where PictureId={srcPicId}");
              Db.Execute($"delete from PicturePerson where PictureId={srcPicId}");
              Db.Execute($"delete from Pictures where Id={srcPicId}");
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

    public void RemoveSelectedFromWeb() {
      var doc = WbThumbs.Document;
      if (doc == null) return;
      foreach (var picture in SelectedPictures) {
        var thumb = doc.GetElementById(picture.Index.ToString());
        if (thumb == null) continue;
        thumb.OuterHtml = string.Empty;
        Pictures.Remove(picture);
      }
      SelectedPictures.Clear();

      var index = 0;
      foreach (var picture in Pictures) {
        var thumb = doc.GetElementById(picture.Index.ToString());
        thumb?.SetAttribute("id", index.ToString());
        picture.Index = index;
        index++;
      }
    }

    #region Static Methods

    public static void DoEvents() {
      DispatcherFrame frame = new DispatcherFrame();
      Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
          new DispatcherOperationCallback(ExitFrame), frame);
      Dispatcher.PushFrame(frame);
    }

    public static object ExitFrame(object f) {
      ((DispatcherFrame)f).Continue = false;
      return null;
    }

    public static void CreateThumbnail(string origPath, string newPath) {
      int size = Settings.Default.ThumbnailSize;
      string dir = Path.GetDirectoryName(newPath);
      if (dir == null) return;
      Directory.CreateDirectory(dir);
      try {

        /*var thumb = new ShellThumbnail();
        thumb.CreateThumbnail(origPath, newPath, size, 80L);
        thumb.Dispose();*/

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
      } catch (Exception) {
        //file can have 0 size
      }
    }

    #endregion
  }
}
