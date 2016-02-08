using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using PictureManager.Properties;

namespace PictureManager.Data {
  public class Picture {
    public string CacheFilePath => FilePath.Replace(":\\", @Settings.Default.CachePath);
    public string FilePath;
    public string FileName;
    public string FileExt;
    public int Index;
    public int Id;
    public int DirId;
    public int Rating;
    public bool IsModifed;
    public DbStuff Db;
    public List<Keyword> Keywords = new List<Keyword>();
    public List<Person> People = new List<Person>();
    public FolderKeyword FolderKeyword;

    public Picture(string filePath, DbStuff db, int index) {
      FilePath = filePath;
      FileName = Path.GetFileName(filePath);
      FileExt = Path.GetExtension(filePath);
      if (!string.IsNullOrEmpty(FileExt))
        FileExt = FileExt.Replace(".", string.Empty).ToLower();
      Index = index;
      Id = -1;
      DirId = -1;
      Db = db;
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

      return sb.ToString();
    }

    public void LoadKeywordsFromDb(Keywords keywords) {
      Keywords.Clear();
      string sql = "select K.Id, K.Keyword from Keywords as K " +
                   $"where K.Id in (select KeywordId from PictureKeyword where PictureId = {Id}) order by Keyword";
      foreach (DataRow row in Db.Select(sql)) {
        Keywords.Add(keywords.GetKeywordByFullPath((string)row[1], false));
      }
    }

    public void LoadPeopleFromDb(People people) {
      People.Clear();
      string sql = "select P.Id, P.Name from People as P " +
                   $"where P.Id in (select PersonId from PicturePerson where PictureId = {Id}) order by Name";
      foreach (DataRow row in Db.Select(sql)) {
        People.Add(people.GetPersonById((int) (long) row[0]));
      }
    }

    public void RefreshFromDb(Keywords keywords, People people) {
      Rating = (int)(long)Db.ExecuteScalar($"select Rating from Pictures where Id = {Id}");
      LoadKeywordsFromDb(keywords);
      LoadPeopleFromDb(people);
    }

    public void LoadFromDb(Keywords keywords, People people) {
      var result = Db.Select($"select Id, Rating from Pictures where DirectoryId = {DirId} and FileName = '{FileName}'");
      if (result != null && result.Count == 1) {
        Id = (int)(long)result[0][0];
        Rating = (int)(long)result[0][1];
        LoadKeywordsFromDb(keywords);
        LoadPeopleFromDb(people);
      } else {
        SavePictureInToDb(keywords, people);
      }
    }

    public void SavePictureInToDb(Keywords keywords, People people) {
      if (Id == -1) {
        ReadMetadata(keywords, people);
        if (Db.Execute($"insert into Pictures (DirectoryId, FileName, Rating) values ({DirId}, '{FileName}', {Rating})")) {
          var id = Db.GetLastIdFor("Pictures");
          if (id != null) Id = (int) id;
        }
      } else {
        Db.Execute($"update Pictures set Rating = {Rating} where Id = {Id}");
      }

      SavePictureKeywordsToDb();
      SavePicturePeopleInToDb();
    }

    public void SavePictureKeywordsToDb() {
      if (Keywords.Count == 0) {
        Db.Execute($"delete from PictureKeyword where PictureId = {Id}");
        return;
      }
      //Update connection between Keywords and Picture
      List<int> keyIds = Keywords.Select(k => k.Id).ToList();
      string keyIdss = keyIds.Aggregate("", (current, id) => current + (id + ","));
      keyIdss = keyIdss.Remove(keyIdss.Length - 1);
      Db.Execute($"delete from PictureKeyword where PictureId = {Id} and KeywordId not in ({keyIdss})");

      //Select existing Keywords for Picture
      foreach (DataRow row in Db.Select($"select KeywordId from PictureKeyword where PictureId = {Id}")) {
        keyIds.Remove((int)(long)row[0]);
      }

      foreach (var keyId in keyIds) {
        Db.Execute($"insert into PictureKeyword (PictureId, KeywordId) values ({Id}, {keyId})");
      }
    }

    public void SavePicturePeopleInToDb() {
      if (People.Count == 0) {
        Db.Execute($"delete from PicturePerson where PictureId = {Id}");
        return;
      }

      //Update connection between People and Picture
      List<int> ids = People.Select(p => p.Id).ToList();
      var idss = ids.Aggregate("", (current, id) => current + (id + ","));
      idss = idss.Remove(idss.Length - 1);
      Db.Execute($"delete from PicturePerson where PictureId = {Id} and PersonId not in ({idss})");

      //Select existing People for Picture and remove them from inserting
      foreach (DataRow row in Db.Select($"select PersonId from PicturePerson where PictureId = {Id}")) {
        ids.Remove((int)(long)row[0]);
      }

      //Insert new people to picture
      foreach (var id in ids) {
        Db.Execute($"insert into PicturePerson (PictureId, PersonId) values ({Id}, {id})");
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
            //Writing all people to picture metadata
            var allPeople = people.GetQuery(microsoftRegionInfo);
            if (allPeople != null)
              metadata.SetQuery(microsoftRegionInfo, allPeople);


            metadata.Rating = Rating;
            metadata.Keywords = new ReadOnlyCollection<string>(Keywords.Select(k => k.FullPath).ToList());

            JpegBitmapEncoder encoder = new JpegBitmapEncoder {QualityLevel = Settings.Default.JpegQualityLevel};
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

    public void ReadMetadata(Keywords keywords, People people) {
      using (FileStream imageFileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        if (imageFileStream.Length == 0) return;
        BitmapDecoder decoder = BitmapDecoder.Create(imageFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
        BitmapMetadata bm = (BitmapMetadata) decoder.Frames[0].Metadata;
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
              People.Add(people.GetPersonByName(personDisplayName.ToString(), true));
            }
          }
        }

        //Rating
        Rating = bm.Rating;

        //Keywords
        Keywords.Clear();
        if (bm.Keywords == null) return;
        //Filter out duplicities
        foreach (var k in bm.Keywords.OrderByDescending(x => x)) {
          if (Keywords.Where(x => x.FullPath.StartsWith(k)).ToList().Count == 0) {
            Keywords.Add(keywords.GetKeywordByFullPath(k, true));
          }
        }
      }
    }

  }
}
