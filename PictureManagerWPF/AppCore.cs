using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Drawing.Imaging;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using mshtml;
using PictureManager.Data;
using PictureManager.Properties;
using PictureManager.ShellStuff;
using Encoder = System.Text.Encoder;

namespace PictureManager {
  public class AppCore {
    private BaseItem _lastSelectedSource;
    public ObservableCollection<BaseItem> FoldersRoot;
    public ObservableCollection<BaseItem> KeywordsRoot;
    public Keywords Keywords;
    public People People;
    public Folders Folders;
    public FavoriteFolders FavoriteFolders;

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

    public WebBrowser WbThumbs;
    public AppInfo AppInfo;
    public bool OneFileOnly;
    public bool ViewerOnly = false; //application was run with file path parameter
    public bool KeywordsEditMode = false;
    
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
      Folders = new Folders {Title = "Folders", IconName = "appbar_folder"};
      FavoriteFolders = new FavoriteFolders { Title = "Favorites", IconName = "appbar_folder_star"};

      People.Load();
      Keywords.Load();
      Folders.AddDrives();
      FavoriteFolders.Load();

      KeywordsRoot = new ObservableCollection<BaseItem> {People, Keywords};
      FoldersRoot = new ObservableCollection<BaseItem> {FavoriteFolders, Folders};
    }

    public void UpdateViewBaseInfo() {
      AppInfo.ViewBaseInfo = $"{Pictures.Count} object(s) / {SelectedPictures.Count} selected";
    }

    public void SetSelectedPictures(string ids) {
      SelectedPictures.Clear();
      CurrentPicture = null;
      if (!string.IsNullOrEmpty(ids)) {
        foreach (var index in ids.Split(',')) {
          SelectedPictures.Add(Pictures[int.Parse(index)]);
        }
        if (SelectedPictures.Count == 1) {
          CurrentPicture = SelectedPictures[0];
        }
      }
      MarkUsedKeywordsAndPeople();
    }

    public void TreeView_KeywordsStackPanel_PreviewMouseDown(object item, MouseButton mouseButton, bool recursive) {
      if (item is Keywords || item is People) return;

      switch (mouseButton) {
        case MouseButton.Left: {
          if (KeywordsEditMode) {

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
      //can by Person or Keyword
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
        }
      }
    }

    public void GetPicturesByFolder(string path) {
      if (!Directory.Exists(path)) return;
      Pictures.Clear();

      var exts = new[] {".jpg", ".jpeg"};
      foreach (
        string file in
          Directory.EnumerateFiles(path)
            .Where(f => exts.Any(x => f.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
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

      string innerSql = string.Join(" union ", sqlList);
      string sql = "select Id, (select Path from Directories as D where D.Id = P.DirectoryId) as Path, FileName, Rating " +
                   $"from Pictures as P where P.Id in ({innerSql}) order by FileName";

      foreach (DataRow row in Db.Select(sql)) {
        string picFullPath = Path.Combine((string)row[1], (string)row[2]);
        if (File.Exists(picFullPath)) {
          Picture pic = new Picture(picFullPath, Db, Pictures.Count) {
            Id = (int) (long) row[0],
            Rating = (int) (long) row[3]
          };
          pic.LoadKeywordsFromDb(Keywords);
          pic.LoadPeopleFromDb(People);
          Pictures.Add(pic);
        }
      }
      CurrentPicture = Pictures.Count == 0 ? null : Pictures[0];
    }

    public void CreateThumbnailsWebPage() {
      //clear thumbnails content div
      if (WbThumbs.Document == null) return;
      WbThumbs.InvokeScript("setContentOfElementById", "thumbnails", "");
      DoEvents();

      //append all pictures to content div
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < Pictures.Count; i++) {
        string thumbPath = Pictures[i].CacheFilePath;
        bool flag = File.Exists(thumbPath);
        if (!flag) CreateThumbnail(Pictures[i].FilePath, thumbPath);
        sb.Append(
          $"<div class=\"thumbBox\" id=\"{i}\"><div class=\"keywords\">{Pictures[i].GetKeywordsAsString()}</div><img src=\"{thumbPath}\" /></div>");
        if (!flag) {
          WbThumbs.InvokeScript("AppendToContentOfElementById", "thumbnails", sb.ToString());
          DoEvents();
          sb.Clear();
        }
      }

      WbThumbs.InvokeScript("AppendToContentOfElementById", "thumbnails", sb.ToString());
      DoEvents();

      ScrollToCurrent();
    }

    public void ScrollToCurrent() {
      if (CurrentPicture == null) return;
      WbThumbs.InvokeScript("scrollToElementById", CurrentPicture.Index);
    }

    public void InitPictures(string dir) {
      int dirId = Db.InsertDirecotryInToDb(dir);
      if (dirId == 0) return;

      for (int i = 0; i < Pictures.Count; i++) {
        Pictures[i].DirId = dirId;
        Pictures[i].LoadFromDb(Keywords, People);
        WbUpdatePictureInfo(i);
        DoEvents();
      }
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
      var doc = (IHTMLDocument3) WbThumbs.Document;
      var thumb = doc?.getElementById(index.ToString());
      if (thumb == null) return;
      foreach (IHTMLElement element in thumb.children) {
        if (element.className != null && element.className.Equals("keywords")) {
          element.innerHTML = Pictures[index].GetKeywordsAsString();
          break;
        }
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
        var thumb = new ShellThumbnail();
        thumb.CreateThumbnail(origPath, newPath, size, 80L);
      } catch (Exception) {
        //file can have 0 size
      }
    }

    #endregion
  }
}
