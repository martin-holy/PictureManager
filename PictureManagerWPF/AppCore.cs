using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using mshtml;
using PictureManager.Data;

namespace PictureManager {
  public class AppCore {
    public Folder FolderBase;
    public Folder FolderCategoryFavorites;
    public DataBase KeywordsBase;
    public DataBase KeywordsCategoryCurrent;
    public DataBase KeywordsCategoryPeople;
    public DataBase KeywordsCategoryKeywords;
    public DataBase LastSelectedSource;
    public bool LastSelectedSourceRecursive;

    public DbStuff Db;
    public ObservableCollection<Picture> Pictures;
    public ObservableCollection<Picture> SelectedPictures;
    public List<DataBase> MarkedTags;

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
      MarkedTags = new List<DataBase>();
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
      CreateCategories();
      CreateFolderBase();
      CreateKeywordsBase();
      LoadPeople();
      LoadKeywords();
      LoadFavorites();
    }

    public void CreateFolderBase() {
      FolderBase = new Folder();
      FolderBase.Items.Add(FolderCategoryFavorites);
      FolderBase.AddDrives();
    }

    public void CreateKeywordsBase() {
      KeywordsBase = new DataBase();
      KeywordsBase.Items.Add(KeywordsCategoryCurrent);
      KeywordsBase.Items.Add(KeywordsCategoryPeople);
      KeywordsBase.Items.Add(KeywordsCategoryKeywords);
    }

    public void CreateCategories() {
      FolderCategoryFavorites = new Folder() { Title = "Favorites", Category = true, ImageName = "appbar_folder_star" };
      KeywordsCategoryCurrent = new DataBase() {Title = "Current", Category = true, ImageName = "appbar_folder" };
      KeywordsCategoryPeople = new DataBase() {Title = "People", Category = true, ImageName = "appbar_people_multiple" };
      KeywordsCategoryKeywords = new DataBase() {Title = "Keywords", Category = true, ImageName = "appbar_tag" };
    }

    public void LoadFavorites() {
      FolderCategoryFavorites.Items.Clear();
      foreach (string path in Properties.Settings.Default.FolderFavorites.OrderBy(x => x)) {
        int lio = path.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase);
        string label = path.Substring(lio + 1, path.Length - lio - 1);
        Folder newItem = new Folder { Title = label, FullPath = path, ImageName = "appbar_folder", Parent = FolderCategoryFavorites };
        FolderCategoryFavorites.Items.Add(newItem);
      }
    }

    public void LoadPeople() {
      KeywordsCategoryPeople.Items.Clear();

      const string sql = "select P.Id, Name, " +
                         "(select count(PP.Id) from PicturePerson as PP where PP.PersonId = P.Id) as PicturesCount " +
                         "from People as P order by Name";

      foreach (DataRow row in Db.Select(sql)) {
        KeywordsCategoryPeople.Items.Add(new Person {
          Id = (int) (long) row[0],
          Title = (string) row[1],
          PicCount = (int) (long) row[2],
          ImageName = "appbar_people"
        });
      }
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

    public void LoadKeywords() {
      KeywordsCategoryKeywords.Items.Clear();

      const string sql =
        "select Id, Keyword, (select count(PK.Id) from PictureKeyword as PK where PK.KeywordId in "
        + "(select XK.Id from Keywords as XK where XK.Keyword like K.Keyword||\"%\")) as PicturesCount, Idx from "
        + "Keywords as K order by Keyword";

      foreach (DataRow row in Db.Select(sql)) {
        Keyword newItem = new Keyword {
          Id = (int) (long) row[0],
          //PicCount = (int) (long) row[2],
          Index = (int) (long) row[3],
          ImageName = "appbar_tag",
          FullPath = (string) row[1]
        };

        if (!newItem.FullPath.Contains("/")) {
          newItem.Title = newItem.FullPath;
          KeywordsCategoryKeywords.Items.Add(newItem);
        } else {
          newItem.Title = newItem.FullPath.Substring(newItem.FullPath.LastIndexOf('/') + 1);
          Keyword parentKeyword = GetKeywordByFullPath(newItem.FullPath.Substring(0, newItem.FullPath.LastIndexOf('/')), false);
          if (parentKeyword == null) continue;
          newItem.Parent = parentKeyword;
          parentKeyword.Items.Add(newItem);
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
      foreach (DataBase item in MarkedTags) {
        item.IsMarked = false;

        switch (item.GetType().Name) {
          case nameof(Person): {
            ((Person) item).PicCount = 0;
            break;
          }
          case nameof(Keyword): {
            ((Keyword) item).PicCount = 0;
            break;
          }
        }
      }
      MarkedTags.Clear();
      var pictures = SelectedPictures.Count == 0 ? Pictures : SelectedPictures;
      foreach (Picture picture in pictures) {
        foreach (Person person in picture.People) {
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

      foreach (DataBase item in MarkedTags) {
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

    public Keyword GetKeywordByFullPath(string fullPath, bool create) {
      DataBase root = KeywordsCategoryKeywords;
      while (true) {
        if (root == null || string.IsNullOrEmpty(fullPath)) return null;
        string[] nodeNames = fullPath.Split('/');
        //if (!nodeNames.Any()) return null;
        Keyword keyword = root.Items.Cast<Keyword>().FirstOrDefault(k => k.Title.Equals(nodeNames[0]));
        if (keyword == null) {
          if (!create) return null;
          keyword = CreateKeyword(root, nodeNames[0]);
        }
        if (nodeNames.Count() > 1) {
          root = keyword;
          fullPath = fullPath.Substring(nodeNames[0].Length + 1);
          continue;
        }
        return keyword;
      }
    }

    public Keyword CreateKeyword(DataBase root, string name) {
      string kFullPath = root.Category ? name : $"{root.FullPath}/{name}";
      if (!Db.Execute($"insert into Keywords (Keyword) values ('{kFullPath}')")) return null;
      int keyId = Db.GetLastIdFor("Keywords");
      if (keyId == 0) return null;
      Keyword newKeyword = new Keyword {
        Id = keyId,
        ImageName = "appbar_tag",
        FullPath = kFullPath,
        Title = name,
        Parent = root as Keyword
      };
      
      root.Items.Add(newKeyword);
      root.Items = new ObservableCollection<DataBase>(root.Items.Cast<Keyword>().OrderBy(k => k.Index).ThenBy(k => k.Title));
      return newKeyword;
    }

    public Person GetPersonByName(string name, bool create) {
      Person p = KeywordsCategoryPeople.Items.Cast<Person>().FirstOrDefault(x => x.Title.Equals(name));
      if (p != null) return p;
      return create ? CreatePerson(name) : null;
    }

    public Person CreatePerson(string name) {
      if (!Db.Execute($"insert into People (Name) values ('{name}')")) return null;
      int id = Db.GetLastIdFor("People");
      if (id == 0) return null;
      Person person = new Person {
        Id = id,
        Title = name,
        ImageName = "appbar_people"
      };
      //TODO do Insert insted Add
      KeywordsCategoryPeople.Items.Add(person);
      return person;
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

    public void GetPicturesByTag(DataBase item, bool recursive) {
      Pictures.Clear();
      string sql = string.Empty;

      switch (item.GetType().Name) {
        case nameof(Person): {
          sql =
            "select Id, (select Path from Directories as D where D.Id = P.DirectoryId) as Path, FileName, Rating " +
            $"from Pictures as P where P.Id in (select PictureId from PicturePerson where PersonId = {((Person) item).Id}) order by FileName";
          break;
        }
        case nameof(Keyword): {
          string s = recursive
            ? $"in (select Id from Keywords where Keyword like \"{((Keyword) item).FullPath}%\")"
            : $"= {((Keyword) item).Id}";
          sql =
            "select Id, (select Path from Directories as D where D.Id = P.DirectoryId) as Path, FileName, Rating " +
            "from Pictures as P where P.Id in (select PictureId from PictureKeyword where " +
            $"KeywordId {s}) order by FileName";
          break;
        }
      }

      foreach (DataRow row in Db.Select(sql)) {
        string picFullPath = Path.Combine((string)row[1], (string)row[2]);
        if (File.Exists(picFullPath)) {
          Picture pic = new Picture(picFullPath, Db, Pictures.Count) {
            Id = (int) (long) row[0],
            Rating = (int) (long) row[3]
          };
          LoadPictureKeywordsFromDb(pic);
          LoadPicturePeopleFromDb(pic);
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
        string thumbPath = GetCachePathFor(Pictures[i].FilePath);
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
        LoadPictureFromDb(Pictures[i]);
        WbUpdatePictureInfo(i);
        DoEvents();
      }
    }

    public void LoadPictureFromDb(Picture pic) {
      var result = Db.Select(
        $"select Id, Rating from Pictures where DirectoryId = {pic.DirId} and FileName = '{pic.FileName}'");
      if (result != null && result.Count == 1) {
        pic.Id = (int)(long)result[0][0];
        pic.Rating = (int)(long)result[0][1];
        LoadPictureKeywordsFromDb(pic);
        LoadPicturePeopleFromDb(pic);
      } else {
        SavePictureInToDb(pic);
      }
    }

    public void SavePictureInToDb(Picture pic) {
      if (pic.Id == -1) {
        GetPictureMetadata(pic);
        if (Db.Execute(
          $"insert into Pictures (DirectoryId, FileName, Rating) values ({pic.DirId}, '{pic.FileName}', {pic.Rating})")) {
          pic.Id = Db.GetLastIdFor("Pictures");
        }
      } else {
        Db.Execute($"update Pictures set Rating = {pic.Rating} where Id = {pic.Id}");
      }

      SavePictureKeywordsToDb(pic);
      SavePicturePeopleInToDb(pic);
    }

    public void SavePictureKeywordsToDb(Picture pic) {
      if (pic.Keywords.Count == 0) {
        Db.Execute($"delete from PictureKeyword where PictureId = {pic.Id}");
        return;
      }
      //Update connection between Keywords and Picture
      List<int> keyIds = pic.Keywords.Select(k => k.Id).ToList();
      string keyIdss = keyIds.Aggregate("", (current, id) => current + (id + ","));
      keyIdss = keyIdss.Remove(keyIdss.Length - 1);
      Db.Execute($"delete from PictureKeyword where PictureId = {pic.Id} and KeywordId not in ({keyIdss})");

      //Select existing Keywords for Picture
      foreach (DataRow row in Db.Select($"select KeywordId from PictureKeyword where PictureId = {pic.Id}")) {
        keyIds.Remove((int)(long)row[0]);
      }

      foreach (var keyId in keyIds) {
        Db.Execute($"insert into PictureKeyword (PictureId, KeywordId) values ({pic.Id}, {keyId})");
      }
    }

    public void SavePicturePeopleInToDb(Picture pic) {
      if (pic.People.Count == 0) {
        Db.Execute($"delete from PicturePerson where PictureId = {pic.Id}");
        return;
      }

      //Update connection between People and Picture
      List<int> ids = pic.People.Select(p => p.Id).ToList();
      var idss = ids.Aggregate("", (current, id) => current + (id + ","));
      idss = idss.Remove(idss.Length - 1);
      Db.Execute($"delete from PicturePerson where PictureId = {pic.Id} and PersonId not in ({idss})");

      //Select existing People for Picture and remove them from inserting
      foreach (DataRow row in Db.Select($"select PersonId from PicturePerson where PictureId = {pic.Id}")) {
        ids.Remove((int)(long)row[0]);
      }

      //Insert new people to picture
      foreach (var id in ids) {
        Db.Execute($"insert into PicturePerson (PictureId, PersonId) values ({pic.Id}, {id})");
      }
    }

    public void SetPictureMetadata(Picture pic) {
      FileInfo original = new FileInfo(pic.FilePath);
      FileInfo newFile = new FileInfo(pic.FilePath.Replace(".", "_newFile."));

      using (FileStream imageFileStream = File.Open(pic.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        BitmapFrame frameCopy = GetBitmapFrame(imageFileStream, pic);
        BitmapMetadata copyMetadata = (BitmapMetadata)frameCopy.Metadata ?? GetNewBitmapMetadata(pic);
        if (copyMetadata == null) return;

        copyMetadata.Rating = pic.Rating;
        copyMetadata.Keywords = new ReadOnlyCollection<string>(pic.Keywords.Select(k => k.FullPath).ToList());

        BitmapEncoder encoder = GetBitmapEncoder(pic);
        if (encoder == null) return;
        encoder.Frames.Add(frameCopy);

        using (FileStream imageFileOutStream = new FileStream(newFile.FullName, FileMode.Create)) {
          encoder.Save(imageFileOutStream);
        }
      }

      original.Delete();
      newFile.MoveTo(original.FullName);
    }

    public void GetPictureMetadata(Picture pic) {
      using (FileStream imageFileStream = new FileStream(pic.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        if (imageFileStream.Length == 0) return;
        BitmapMetadata bm = (BitmapMetadata)GetBitmapFrame(imageFileStream, pic).Metadata;
        if (bm == null) return;

        //People
        int i = 0;
        object value;
        pic.People.Clear();
        do {
          value = bm.GetQuery($"/xmp/MP:RegionInfo/MPRI:Regions/{{ulong={i}}}/MPReg:PersonDisplayName");
          if (value != null) {
            pic.People.Add(GetPersonByName(value.ToString(), true));
          }
          i++;
        } while (value != null);

        //Rating
        pic.Rating = bm.Rating;

        //Keywords
        pic.Keywords.Clear();
        if (bm.Keywords == null) return;
        //Filter out duplicities
        foreach (var k in bm.Keywords.OrderByDescending(x => x)) {
          if (pic.Keywords.Where(x => x.FullPath.StartsWith(k)).ToList().Count == 0) {
            pic.Keywords.Add(GetKeywordByFullPath(k, true));
          }
        }
      }
    }

    private BitmapFrame GetBitmapFrame(Stream file, Picture pic) {
      BitmapDecoder bd = null;

      switch (pic.FileExt) {
        case "jpg":
        case "jpeg":
          {
            bd = new JpegBitmapDecoder(
              file,
              BitmapCreateOptions.PreservePixelFormat,
              BitmapCacheOption.None);
            break;
          }
        case "png":
          {
            bd = new PngBitmapDecoder(
              file,
              BitmapCreateOptions.PreservePixelFormat,
              BitmapCacheOption.None);
            break;
          }
      }

      return bd == null ? null : BitmapFrame.Create(bd.Frames[0]);
    }

    private BitmapEncoder GetBitmapEncoder(Picture pic) {
      BitmapEncoder be = null;
      switch (pic.FileExt) {
        case "jpg":
        case "jpeg":
          {
            be = new JpegBitmapEncoder();
            ((JpegBitmapEncoder)be).QualityLevel = Properties.Settings.Default.JpegQualityLevel;
            break;
          }
        case "png":
          {
            be = new PngBitmapEncoder();
            break;
          }
      }
      return be;
    }

    private BitmapMetadata GetNewBitmapMetadata(Picture pic) {
      BitmapMetadata bm = null;
      switch (pic.FileExt) {
        case "jpg":
        case "jpeg":
          {
            bm = new BitmapMetadata("jpg");
            break;
          }
        case "png":
          {
            bm = new BitmapMetadata("png");
            break;
          }
      }
      return bm;
    }

    public void LoadPictureKeywordsFromDb(Picture pic) {
      pic.Keywords.Clear();
      string sql = "select K.Id, K.Keyword from Keywords as K " +
                   $"where K.Id in (select KeywordId from PictureKeyword where PictureId = {pic.Id}) order by Keyword";
      foreach (DataRow row in Db.Select(sql)) {
        pic.Keywords.Add(GetKeywordByFullPath((string) row[1], false));
      }
    }

    public void LoadPicturePeopleFromDb(Picture pic) {
      pic.People.Clear();
      string sql = "select P.Id, P.Name from People as P " +
                   $"where P.Id in (select PersonId from PicturePerson where PictureId = {pic.Id}) order by Name";
      foreach (DataRow row in Db.Select(sql)) {
        foreach (Person p in KeywordsCategoryPeople.Items) {
          if (p.Id == (int) (long) row[0]) {
            pic.People.Add(p);
            break;
          }
        }
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

    public void TreeView_ExpandTo(ObservableCollection<DataBase> items, string path) {
      foreach (
        Folder item in
          items.Cast<Folder>()
            .Where(item => item.FullPath != null && path.StartsWith(item.FullPath, StringComparison.OrdinalIgnoreCase))) {
        if (item.Items.Count != 0) {
          item.IsExpanded = true;
          
        }
        if (path.Equals(item.FullPath, StringComparison.OrdinalIgnoreCase)) {
          //item.IsSelected = true;

        } else {
          TreeView_ExpandTo(item.Items, path);
        }
        break;
      }
    }

    public void TreeView_FoldersOnSelected(Folder item) {
      if (!item.Accessible || item.Category) {
        item.IsSelected = false;
        return;
      }
      if (item.Parent != null && item.Parent.Title.Equals("Favorites")) {
        TreeView_ExpandTo(FolderBase.Items, item.FullPath);
        return;
      }

      LastSelectedSource = item;
      GetPicturesByFolder(item.FullPath);
      CreateThumbnailsWebPage();
      //TODO: tohle dat asi do jineho vlakna
      InitPictures(item.FullPath);
      MarkUsedKeywordsAndPeople();
    }

    public void TreeView_KeywordsOnSelected(DataBase item, bool recursive) {
      if (!item.Accessible || item.Category) {
        item.IsSelected = false;
        return;
      }

      if (KeywordsEditMode) {
        item.IsMarked = !item.IsMarked;
        item.IsSelected = false;
        if (item.IsMarked) MarkedTags.Add(item);
        else MarkedTags.Remove(item);

        foreach (Picture picture in SelectedPictures) {
          picture.Modifed = true;
        }

        switch (item.GetType().Name) {
          case nameof(Person): {
            foreach (Picture picture in SelectedPictures) {
              if (item.IsMarked) {
                picture.People.Add((Person) item);
              } else {
                picture.People.Remove((Person) item);
              }
              WbUpdatePictureInfo(picture.Index);
            }
            break;
          }
          case nameof(Keyword): {
            foreach (Picture picture in SelectedPictures) {
              if (item.IsMarked) {
                for (int i = picture.Keywords.Count - 1; i >= 0; i--) {
                  if (((Keyword) item).FullPath.StartsWith(picture.Keywords[i].FullPath)) {
                    UpdateKeywordPictureCount(picture.Keywords[i], false);
                    picture.Keywords.RemoveAt(i);
                  }
                }
                picture.Keywords.Add((Keyword) item);
                UpdateKeywordPictureCount((Keyword) item, true);
              } else {
                picture.Keywords.Remove((Keyword) item);
                  UpdateKeywordPictureCount((Keyword)item, false);
                }
              WbUpdatePictureInfo(picture.Index);
              MarkUsedKeywordsAndPeople();
            }
            break;
          }
        }
      } else {
        LastSelectedSource = item;
        LastSelectedSourceRecursive = recursive;
        GetPicturesByTag(item, recursive);
        CreateThumbnailsWebPage();
        MarkUsedKeywordsAndPeople();
      }
    }

    public void UpdateKeywordPictureCount(Keyword keyword, bool increase) {
      do {
        keyword.PicCount = increase ? keyword.PicCount + 1 : keyword.PicCount - 1;
        keyword = keyword.Parent;
      } while (keyword != null);
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

    public static string GetCachePathFor(string path) {
      return path.Replace(":\\", @Properties.Settings.Default.CachePath);
    }

    public static void CreateThumbnail(string origPath, string newPath) {
      int size = Properties.Settings.Default.ThumbnailSize;
      string dir = Path.GetDirectoryName(newPath);
      if (dir == null) return;
      Directory.CreateDirectory(dir);
      try {
        KodeSharp.SharedClasses.ShellThumbnail st = new KodeSharp.SharedClasses.ShellThumbnail();
        st.GetThumbnail(origPath, size, size).Save(newPath, System.Drawing.Imaging.ImageFormat.Jpeg);
        st.Dispose();
        //TODO dodelat uvoleni pameni nefunguje ani Dispose ani GC.Colect
      } catch (Exception) {
        //file can have 0 size
      }
    }

    #endregion
  }
}
