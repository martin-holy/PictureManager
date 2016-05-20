using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using PictureManager.Properties;

namespace PictureManager.ViewModel {
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
    public DataModel.PmDataContext Db;
    public DataModel.MediaItem Data;
    public List<Keyword> Keywords = new List<Keyword>();
    public List<Person> People = new List<Person>();
    public FolderKeyword FolderKeyword;
    public WebBrowser WbThumbs;

    public BaseMediaItem(string filePath, DataModel.PmDataContext db, int index, WebBrowser wbThumbs, DataModel.MediaItem data) {
      FilePath = filePath;
      Db = db;
      Index = index;
      WbThumbs = wbThumbs;

      if (data == null) return;
      Id = data.Id;
      DirId = data.DirectoryId;
      Comment = data.Comment;
      Rating = data.Rating;
      Orientation = data.Orientation;
      Data = data;
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

    private void LoadKeywordsFromDb(Keywords keywords) {
      Keywords.Clear();
      var ks = from mik in Db.MediaItemKeywords.Where(x => x.MediaItemId == Id)
        join k in Db.Keywords on mik.KeywordId equals k.Id
        select k.Name;
      foreach (var k in ks) {
        Keywords.Add(keywords.GetKeywordByFullPath(k, false));
      }
    }

    private void LoadPeopleFromDb(People people) {
      People.Clear();
      var ps = from mip in Db.MediaItemPeople.Where(x => x.MediaItemId == Id)
        join p in Db.People on mip.PersonId equals p.Id
        select p;
      foreach (var p in ps) {
        People.Add(people.GetPerson(p.Id, p.PeopleGroupId));
      }
    }

    public void ReLoadFromDb(AppCore aCore, DataModel.MediaItem mi) {
      if (mi == null) return;
      Rating = mi.Rating;
      Comment = mi.Comment;
      Orientation = mi.Orientation;
      LoadKeywordsFromDb(aCore.Keywords);
      LoadPeopleFromDb(aCore.People);
    }

    public void SaveMediaItemInToDb(AppCore aCore, bool update, bool isNew) {
      if (isNew) {
        ReadMetadata(aCore);
        Id = Db.GetNextIdFor("MediaItems");
        Data = new DataModel.MediaItem {
          Id = Id,
          DirectoryId = DirId,
          FileName = FileNameWithExt,
          Rating = Rating,
          Comment = Comment,
          Orientation = Orientation
        };
        Db.InsertOnSubmit(Data);
      } else {
        if (update) ReadMetadata(aCore);
        Data.Rating = Rating;
        Data.Comment = CommentEscaped;
        Data.Orientation = Orientation;
        Db.UpdateOnSubmit(Data);
      }

      SaveMediaItemKeywordsToDb();
      SaveMediaItemPeopleInToDb();
    }

    public void SaveMediaItemKeywordsToDb() {
      //Update connection between Keywords and MediaItem
      var keyIds = Keywords.Select(k => k.Id).ToList();
      foreach (var mik in Db.MediaItemKeywords.Where(x => x.MediaItemId == Id)) {
        if (Keywords.FirstOrDefault(x => x.Id == mik.KeywordId) == null)
          Db.DeleteOnSubmit(mik);
        else
          keyIds.Remove(mik.KeywordId);
      }
      //Insert new Keywords to MediaItem
      foreach (var keyId in keyIds) {
        Db.InsertOnSubmit(new DataModel.MediaItemKeyword {
          Id = Db.GetNextIdFor("MediaItemKeyword"),
          KeywordId = keyId,
          MediaItemId = Id
        });
      }
    }

    public void SaveMediaItemPeopleInToDb() {
      //Update connection between People and MediaItem
      var ids = People.Select(p => p.Id).ToList();
      foreach (var mip in Db.MediaItemPeople.Where(x => x.MediaItemId == Id)) {
        if (People.FirstOrDefault(x => x.Id == mip.PersonId) == null) 
          Db.DeleteOnSubmit(mip);
         else
          ids.Remove(mip.PersonId);
      }
      //Insert new People to MediaItem
      foreach (var id in ids) {
        Db.InsertOnSubmit(new DataModel.MediaItemPerson {
          Id = Db.GetNextIdFor("MediaItemPerson"),
          PersonId = id,
          MediaItemId = Id
        });
      }
    }

    public void ReSave() {
      //TODO: try to preserve EXIF information
      FileInfo original = new FileInfo(FilePath);
      FileInfo newFile = new FileInfo(FilePath + "_newFile");
      try {
        using (Stream originalFileStream = File.Open(original.FullName, FileMode.Open, FileAccess.Read)) {
          using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(originalFileStream)) {
            using (Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
              ImageCodecInfo encoder = ImageCodecInfo.GetImageDecoders().SingleOrDefault(x => x.FormatID == bmp.RawFormat.Guid);
              if (encoder == null) return;
              EncoderParameters encParams = new EncoderParameters(1) {
                Param = {[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, Settings.Default.JpegQualityLevel)}
              };
              bmp.Save(newFileStream, encoder, encParams);
            }
          }
        }

        newFile.CreationTime = original.CreationTime;
        original.Delete();
        newFile.MoveTo(original.FullName);
      }
      catch (Exception) {
        if (newFile.Exists) newFile.Delete();
      }
    }

    public bool TryWriteMetadata() {
      var bSuccess = WriteMetadata();
      if (bSuccess) return true;
      ReSave();
      return WriteMetadata();
    }

    public bool WriteMetadata() {
      FileInfo original = new FileInfo(FilePath);
      FileInfo newFile = new FileInfo(FilePath + "_newFile");
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
            metadata.Comment = Comment ?? string.Empty;
            metadata.Keywords = new ReadOnlyCollection<string>(Keywords.Select(k => k.FullPath).ToList());

            JpegBitmapEncoder encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };
            encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0], decoder.Frames[0].Thumbnail, metadata,
              decoder.Frames[0].ColorContexts));

            try {
              using (Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
                encoder.Save(newFileStream);
              }
              bSuccess = true;
            }
            catch (Exception) {
              bSuccess = false;
            }
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
                People.Add(aCore.People.GetPerson(personDisplayName.ToString(), true));
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
