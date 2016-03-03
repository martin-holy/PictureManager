using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using PictureManager.Properties;

namespace PictureManager.Data {
  public class BaseMediaItem: INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private bool _isSelected;
    public bool IsSelected {
      get {
        return _isSelected;
      }
      set {
        _isSelected = value;

        WbThumbs.Document?.GetElementById(Index.ToString())?.SetAttribute("className", value ? "thumbBox selected" : "thumbBox");
        OnPropertyChanged();
      }
    }
    
    public string FilePath;
    public string FilePathCache => FilePath.Replace(":\\", @Settings.Default.CachePath);
    public string FileNameWithExt => Path.GetFileName(FilePath);
    public string Comment;
    public string CommentEscaped => Comment?.Replace("'", "''");
    public int Index;
    public int Id;
    public int DirId;
    public int Rating;
    public int Orientation;
    public bool IsModifed;
    public DbStuff Db;
    public List<Keyword> Keywords = new List<Keyword>();
    public List<Person> People = new List<Person>();
    public FolderKeyword FolderKeyword;
    public WebBrowser WbThumbs;

    public BaseMediaItem(string filePath, DbStuff db, int index, WebBrowser wbThumbs) {
      FilePath = filePath;
      Db = db;
      Index = index;
      WbThumbs = wbThumbs;
      Id = -1;
      DirId = -1;
    }

    public string GetKeywordsAsString() {
      StringBuilder sb = new StringBuilder();

      foreach (Person p in People.OrderBy(x => x.Title)) {
        sb.Append("<div>");
        sb.Append(p.Title);
        sb.Append("</div>");
      }

      List<string> keywordsList = new List<string>();
      foreach (Keyword keyword in Keywords.OrderBy(x => x.FullPath)) {
        foreach (string k in keyword.FullPath.Split('/')) {
          if (!keywordsList.Contains(k)) keywordsList.Add(k);
        }
      }

      foreach (var keyword in keywordsList) {
        sb.Append("<div>");
        sb.Append(keyword);
        sb.Append("</div>");
      }

      sb.Append("<div>");
      sb.Append(Rating);
      sb.Append("</div>");

      if (Comment != string.Empty) {
        sb.Append("<div>C</div>");
      }

      return sb.ToString();
    }

    public void LoadKeywordsFromDb(Keywords keywords) {
      Keywords.Clear();
      string sql = "select K.Id, K.Keyword from Keywords as K " +
                   $"where K.Id in (select KeywordId from MediaItemKeyword where MediaItemId = {Id}) order by Keyword";
      foreach (DataRow row in Db.Select(sql)) {
        Keywords.Add(keywords.GetKeywordByFullPath((string)row[1], false));
      }
    }

    public void LoadPeopleFromDb(People people) {
      People.Clear();
      string sql = "select P.Id, P.Name from People as P " +
                   $"where P.Id in (select PersonId from MediaItemPerson where MediaItemId = {Id}) order by Name";
      foreach (DataRow row in Db.Select(sql)) {
        People.Add(people.GetPersonById((int)(long)row[0]));
      }
    }

    public void LoadFromDb(AppCore aCore) {
      var result = Db.Select($"select Id, Rating, Comment, Orientation from MediaItems where DirectoryId = {DirId} and FileName = '{FileNameWithExt}'");
      if (result != null && result.Count == 1) {
        Id = (int)(long)result[0][0];
        Rating = (int)(long)result[0][1];
        Comment = (string)result[0][2];
        Orientation = (int)(long)result[0][3];
        LoadKeywordsFromDb(aCore.Keywords);
        LoadPeopleFromDb(aCore.People);
      } else {
        SaveMediaItemInToDb(aCore, false);
      }
    }

    public void SaveMediaItemInToDb(AppCore aCore, bool update) {
      if (Id == -1) {
        ReadMetadata(aCore);
        if (Db.Execute($"insert into MediaItems (DirectoryId, FileName, Rating, Comment, Orientation) values ({DirId}, '{FileNameWithExt}', {Rating}, '{CommentEscaped}', {Orientation})")) {
          var id = Db.GetLastIdFor("MediaItems");
          if (id != null) Id = (int)id;
        }
      } else {
        if (update) ReadMetadata(aCore);
        Db.Execute($"update MediaItems set Rating = {Rating}, Comment = '{CommentEscaped}', Orientation = {Orientation} where Id = {Id}");
      }

      SaveMediaItemKeywordsToDb();
      SaveMediaItemPeopleInToDb();
    }

    public void SaveMediaItemKeywordsToDb() {
      if (Keywords.Count == 0) {
        Db.Execute($"delete from MediaItemKeyword where MediaItemId = {Id}");
        return;
      }
      //Update connection between Keywords and MediaItem
      List<int> keyIds = Keywords.Select(k => k.Id).ToList();
      string keyIdss = keyIds.Aggregate("", (current, id) => current + (id + ","));
      keyIdss = keyIdss.Remove(keyIdss.Length - 1);
      Db.Execute($"delete from MediaItemKeyword where MediaItemId = {Id} and KeywordId not in ({keyIdss})");

      //Select existing Keywords for MediaItem
      foreach (DataRow row in Db.Select($"select KeywordId from MediaItemKeyword where MediaItemId = {Id}")) {
        keyIds.Remove((int)(long)row[0]);
      }

      //Insert new Keywords to MediaItem
      foreach (var keyId in keyIds) {
        Db.Execute($"insert into MediaItemKeyword (MediaItemId, KeywordId) values ({Id}, {keyId})");
      }
    }

    public void SaveMediaItemPeopleInToDb() {
      if (People.Count == 0) {
        Db.Execute($"delete from MediaItemPerson where MediaItemId = {Id}");
        return;
      }

      //Update connection between People and MediaItem
      List<int> ids = People.Select(p => p.Id).ToList();
      var idss = ids.Aggregate("", (current, id) => current + (id + ","));
      idss = idss.Remove(idss.Length - 1);
      Db.Execute($"delete from MediaItemPerson where MediaItemId = {Id} and PersonId not in ({idss})");

      //Select existing People for MediaItem and remove them from inserting
      foreach (DataRow row in Db.Select($"select PersonId from MediaItemPerson where MediaItemId = {Id}")) {
        ids.Remove((int)(long)row[0]);
      }

      //Insert new People to MediaItem
      foreach (var id in ids) {
        Db.Execute($"insert into MediaItemPerson (MediaItemId, PersonId) values ({Id}, {id})");
      }
    }

    public bool WriteMetadata() {
      FileInfo original = new FileInfo(FilePath);
      FileInfo newFile = new FileInfo(FilePath.Replace(".", "_newFile."));
      bool bSuccess = false;
      const BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;

      using (Stream originalFileStream = File.Open(original.FullName, FileMode.Open, FileAccess.Read)) {
        BitmapDecoder decoder = BitmapDecoder.Create(originalFileStream, createOptions, BitmapCacheOption.None);
        if (decoder.CodecInfo != null && decoder.CodecInfo.FileExtensions.Contains("jpg") && decoder.Frames[0] != null) {
          BitmapMetadata metadata = decoder.Frames[0].Metadata == null
            ? new BitmapMetadata("jpg")
            : decoder.Frames[0].Metadata.Clone() as BitmapMetadata;

          if (metadata != null) {

            //People
            const string microsoftRegionInfo = @"/xmp/MP:RegionInfo";
            const string microsoftRegions = @"/xmp/MP:RegionInfo/MPRI:Regions";
            const string microsoftPersonDisplayName = @"/MPReg:PersonDisplayName";
            int peopleIdx = -1;
            List<string> addedPeople = new List<string>();
            //New metadata just for People
            BitmapMetadata people = new BitmapMetadata("jpg");
            people.SetQuery(microsoftRegionInfo, new BitmapMetadata("xmpstruct"));
            people.SetQuery(microsoftRegions, new BitmapMetadata("xmpbag"));
            //Adding existing people
            BitmapMetadata existingPeople = metadata.GetQuery(microsoftRegions) as BitmapMetadata;
            if (existingPeople != null) {
              foreach (string idx in existingPeople) {
                var existingPerson = metadata.GetQuery(microsoftRegions + idx) as BitmapMetadata;
                var personDisplayName = existingPerson?.GetQuery(microsoftPersonDisplayName);
                if (personDisplayName == null) continue;
                if (!People.Any(p => p.Title.Equals(personDisplayName.ToString()))) continue;
                addedPeople.Add(personDisplayName.ToString());
                peopleIdx++;
                people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}", existingPerson);
              }
            }
            //Adding new people
            foreach (Person person in People.Where(p => !addedPeople.Any(ap => ap.Equals(p.Title)))) {
              peopleIdx++;
              people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}", new BitmapMetadata("xmpstruct"));
              people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}" + microsoftPersonDisplayName, person.Title);
            }
            //Writing all people to MediaItem metadata
            var allPeople = people.GetQuery(microsoftRegionInfo);
            if (allPeople != null)
              metadata.SetQuery(microsoftRegionInfo, allPeople);


            metadata.Rating = Rating;
            metadata.Comment = Comment;
            metadata.Keywords = new ReadOnlyCollection<string>(Keywords.Select(k => k.FullPath).ToList());

            JpegBitmapEncoder encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };
            encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0], decoder.Frames[0].Thumbnail, metadata,
              decoder.Frames[0].ColorContexts));

            using (Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
              encoder.Save(newFileStream);
            }
            bSuccess = true;
          }
        }
      }

      if (bSuccess) {
        newFile.CreationTime = original.CreationTime;
        original.Delete();
        newFile.MoveTo(original.FullName);
      }
      return bSuccess;
    }

    public void ReadMetadata(AppCore aCore) {
      try {
        using (FileStream imageFileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
          if (imageFileStream.Length == 0) return;
          BitmapDecoder decoder = BitmapDecoder.Create(imageFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
          BitmapMetadata bm = (BitmapMetadata)decoder.Frames[0].Metadata;
          if (bm == null) return;

          //People
          People.Clear();
          const string microsoftRegions = @"/xmp/MP:RegionInfo/MPRI:Regions";
          const string microsoftPersonDisplayName = @"/MPReg:PersonDisplayName";

          var regions = bm.GetQuery(microsoftRegions) as BitmapMetadata;
          if (regions != null) {
            foreach (string region in regions) {
              var personDisplayName = bm.GetQuery(microsoftRegions + region + microsoftPersonDisplayName);
              if (personDisplayName != null) {
                People.Add(aCore.People.GetPersonByName(personDisplayName.ToString(), true));
              }
            }
          }

          //Rating
          Rating = bm.Rating;

          //Comment
          Comment = bm.Comment == null
            ? string.Empty
            : aCore.IncorectChars.Aggregate(bm.Comment, (current, ch) => current.Replace(ch, string.Empty));

          //Orientation
          var orientation = bm.GetQuery("System.Photo.Orientation");
          if (orientation != null) {
            //3: 180, 6: 270, 8: 90
            Orientation = (ushort) orientation;
          }

          //Keywords
          Keywords.Clear();
          if (bm.Keywords == null) return;
          //Filter out duplicities
          foreach (var k in bm.Keywords.OrderByDescending(x => x)) {
            if (Keywords.Where(x => x.FullPath.StartsWith(k)).ToList().Count == 0) {
              var keyword = aCore.Keywords.GetKeywordByFullPath(k, true);
              if (keyword != null)
                Keywords.Add(keyword);
            }
          }
        }
      } catch (Exception) {
        // ignored
      }
    }

    public void WbUpdateInfo() {
      var thumb = WbThumbs.Document?.GetElementById(Index.ToString());
      if (thumb == null) return;
      foreach (HtmlElement element in thumb.Children) {
        if (!element.GetAttribute("className").Equals("keywords")) continue;
        element.InnerHtml = GetKeywordsAsString();
        break;
      }
    }
  }
}
