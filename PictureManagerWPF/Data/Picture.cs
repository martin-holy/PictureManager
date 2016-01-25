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
        GetPictureMetadata(keywords, people);
        if (Db.Execute(
          $"insert into Pictures (DirectoryId, FileName, Rating) values ({DirId}, '{FileName}', {Rating})")) {
          Id = Db.GetLastIdFor("Pictures");
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

    public void SetPictureMetadata() {
      FileInfo original = new FileInfo(FilePath);
      FileInfo newFile = new FileInfo(FilePath.Replace(".", "_newFile."));

      using (FileStream imageFileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        BitmapFrame frameCopy = GetBitmapFrame(imageFileStream);
        BitmapMetadata copyMetadata = (BitmapMetadata)frameCopy.Metadata ?? GetNewBitmapMetadata();
        if (copyMetadata == null) return;

        copyMetadata.Rating = Rating;
        copyMetadata.Keywords = new ReadOnlyCollection<string>(Keywords.Select(k => k.FullPath).ToList());

        BitmapEncoder encoder = GetBitmapEncoder();
        if (encoder == null) return;
        encoder.Frames.Add(frameCopy);

        using (FileStream imageFileOutStream = new FileStream(newFile.FullName, FileMode.Create)) {
          encoder.Save(imageFileOutStream);
        }
      }

      original.Delete();
      newFile.MoveTo(original.FullName);
    }

    public void GetPictureMetadata(Keywords keywords, People people) {
      using (FileStream imageFileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
        if (imageFileStream.Length == 0) return;
        BitmapMetadata bm = (BitmapMetadata)GetBitmapFrame(imageFileStream).Metadata;
        if (bm == null) return;

        //People
        int i = 0;
        object value;
        People.Clear();
        do {
          value = bm.GetQuery($"/xmp/MP:RegionInfo/MPRI:Regions/{{ulong={i}}}/MPReg:PersonDisplayName");
          if (value != null) {
            People.Add(people.GetPersonByName(value.ToString(), true));
          }
          i++;
        } while (value != null);

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

    private BitmapFrame GetBitmapFrame(Stream file) {
      BitmapDecoder bd = null;

      switch (FileExt) {
        case "jpg":
        case "jpeg": {
            bd = new JpegBitmapDecoder(
              file,
              BitmapCreateOptions.PreservePixelFormat,
              BitmapCacheOption.None);
            break;
          }
        case "png": {
            bd = new PngBitmapDecoder(
              file,
              BitmapCreateOptions.PreservePixelFormat,
              BitmapCacheOption.None);
            break;
          }
      }

      return bd == null ? null : BitmapFrame.Create(bd.Frames[0]);
    }

    private BitmapEncoder GetBitmapEncoder() {
      BitmapEncoder be = null;
      switch (FileExt) {
        case "jpg":
        case "jpeg": {
            be = new JpegBitmapEncoder();
            ((JpegBitmapEncoder)be).QualityLevel = Settings.Default.JpegQualityLevel;
            break;
          }
        case "png": {
            be = new PngBitmapEncoder();
            break;
          }
      }
      return be;
    }

    private BitmapMetadata GetNewBitmapMetadata() {
      BitmapMetadata bm = null;
      switch (FileExt) {
        case "jpg":
        case "jpeg": {
            bm = new BitmapMetadata("jpg");
            break;
          }
        case "png": {
            bm = new BitmapMetadata("png");
            break;
          }
      }
      return bm;
    }



  }
}
