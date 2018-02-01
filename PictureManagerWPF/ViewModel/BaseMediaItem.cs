using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using PictureManager.Properties;
using Application = System.Windows.Application;

namespace PictureManager.ViewModel {
  public class BaseMediaItem: INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private bool _isSelected;
    public bool IsSelected {
      get => _isSelected;
      set {
        _isSelected = value;
        //BUG: pri slideshow nenacte Document
        ACore.WbThumbs.Document?.GetElementById(Index.ToString())?.SetAttribute("className", value ? "thumbBox selected" : "thumbBox");
        OnPropertyChanged();
      }
    }
    
    public string FilePath;
    public string FilePathCache => FilePath.Replace(":\\", Settings.Default.CachePath);
    public string FileNameWithExt => Path.GetFileName(FilePath);
    public string Comment;
    public string CommentEscaped => Comment?.Replace("'", "''");
    public int Index;
    public int Id;
    public int DirId;
    public int Rating;
    public int Orientation;
    public int Width;
    public int Height;
    public int? GeoNameId;
    public double? Lat;
    public double? Lng;
    public MediaTypes MediaType;
    public bool IsModifed;
    public DataModel.MediaItem Data;
    public List<Keyword> Keywords = new List<Keyword>();
    public List<Person> People = new List<Person>();
    public FolderKeyword FolderKeyword;
    public AppCore ACore;

    public BaseMediaItem(string filePath, int index, DataModel.MediaItem data) {
      ACore = ACore = (AppCore) Application.Current.Properties[nameof(AppProps.AppCore)];
      FilePath = filePath;
      Index = index;
      MediaType = ACore.MediaItems.SuportedImageExts.Any(
        e => filePath.EndsWith(e, StringComparison.InvariantCultureIgnoreCase))
        ? MediaTypes.Image
        : MediaTypes.Video;

      if (data == null) return;
      Id = data.Id;
      DirId = data.DirectoryId;
      Comment = data.Comment;
      Rating = data.Rating;
      Orientation = data.Orientation;
      Width = data.Width;
      Height = data.Height;
      GeoNameId = data.GeoNameId;
      Data = data;
    }

    public string GetKeywordsAsString(bool withComment) {
      var sb = new StringBuilder();

      foreach (var p in People.OrderBy(x => x.Title)) {
        sb.Append("<div>");
        sb.Append(p.Title);
        sb.Append("</div>");
      }

      var keywordsList = new List<string>();
      foreach (var keyword in Keywords.OrderBy(x => x.FullPath)) {
        foreach (var k in keyword.FullPath.Split('/')) {
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
        sb.Append("<div>");
        sb.Append(withComment ? CommentEscaped : "C");
        sb.Append("</div>");
      }

      if (GeoNameId != null) sb.Append("<div>G</div>");
      if (MediaType == MediaTypes.Video) sb.Append("<div>V</div>");

      return sb.ToString();
    }

    private void LoadKeywordsFromDb() {
      Keywords.Clear();
      var ks = from mik in ACore.Db.MediaItemKeywords.Where(x => x.MediaItemId == Id)
        join k in ACore.Db.Keywords on mik.KeywordId equals k.Id
        select k.Name;
      foreach (var k in ks) {
        Keywords.Add(ACore.Keywords.GetKeywordByFullPath(k, false));
      }
    }

    private void LoadPeopleFromDb() {
      People.Clear();
      var ps = from mip in ACore.Db.MediaItemPeople.Where(x => x.MediaItemId == Id)
        join p in ACore.Db.People on mip.PersonId equals p.Id
        select p;
      foreach (var p in ps) {
        People.Add(ACore.People.GetPerson(p.Id));
      }
    }

    public void ReLoadFromDb() {
      ACore.Db.ReloadItem(Data);
      Rating = Data.Rating;
      Comment = Data.Comment;
      Orientation = Data.Orientation;
      LoadKeywordsFromDb();
      LoadPeopleFromDb();
      IsModifed = false;
    }

    public void SaveMediaItemInToDb(bool update, bool isNew, List<DataModel.BaseTable>[] lists) {
      if (isNew) {
        ReadMetadata();
        Id = ACore.Db.GetNextIdFor<DataModel.MediaItem>();
        Data = new DataModel.MediaItem {
          Id = Id,
          DirectoryId = DirId,
          FileName = FileNameWithExt,
          Rating = Rating,
          Comment = Comment,
          Orientation = Orientation,
          Width = Width,
          Height = Height,
          GeoNameId = GeoNameId
        };
        ACore.Db.InsertOnSubmit(Data, lists);
      } else {
        if (update) ReadMetadata();
        Data.Rating = Rating;
        Data.Comment = CommentEscaped;
        Data.Orientation = Orientation;
        Data.GeoNameId = GeoNameId;
        Data.Width = Width;
        Data.Height = Height;
        Data.GeoNameId = GeoNameId;
        ACore.Db.UpdateOnSubmit(Data, lists);
      }

      SaveMediaItemKeywordsToDb(lists);
      SaveMediaItemPeopleInToDb(lists);
    }

    public void SaveMediaItemKeywordsToDb(List<DataModel.BaseTable>[] lists) {
      //Update connection between Keywords and MediaItem
      var keyIds = Keywords.Select(k => k.Id).ToList();
      foreach (var mik in ACore.Db.MediaItemKeywords.Where(x => x.MediaItemId == Id)) {
        if (Keywords.FirstOrDefault(x => x.Id == mik.KeywordId) == null)
          ACore.Db.DeleteOnSubmit(mik, lists);
        else
          keyIds.Remove(mik.KeywordId);
      }
      //Insert new Keywords to MediaItem
      foreach (var keyId in keyIds) {
        ACore.Db.InsertOnSubmit(new DataModel.MediaItemKeyword {
          Id = ACore.Db.GetNextIdFor<DataModel.MediaItemKeyword>(),
          KeywordId = keyId,
          MediaItemId = Id
        }, lists);
      }
    }

    public void SaveMediaItemPeopleInToDb(List<DataModel.BaseTable>[] lists) {
      //Update connection between People and MediaItem
      var ids = People.Select(p => p.Id).ToList();
      foreach (var mip in ACore.Db.MediaItemPeople.Where(x => x.MediaItemId == Id)) {
        if (People.FirstOrDefault(x => x.Id == mip.PersonId) == null)
          ACore.Db.DeleteOnSubmit(mip, lists);
         else
          ids.Remove(mip.PersonId);
      }
      //Insert new People to MediaItem
      foreach (var id in ids) {
        ACore.Db.InsertOnSubmit(new DataModel.MediaItemPerson {
          Id = ACore.Db.GetNextIdFor<DataModel.MediaItemPerson>(),
          PersonId = id,
          MediaItemId = Id
        }, lists);
      }
    }

    public void ReSave() {
      //TODO: try to preserve EXIF information
      var original = new FileInfo(FilePath);
      var newFile = new FileInfo(FilePath + "_newFile");
      try {
        using (Stream originalFileStream = File.Open(original.FullName, FileMode.Open, FileAccess.Read)) {
          using (var bmp = new System.Drawing.Bitmap(originalFileStream)) {
            using (Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
              var encoder = ImageCodecInfo.GetImageDecoders().SingleOrDefault(x => x.FormatID == bmp.RawFormat.Guid);
              if (encoder == null) return;
              var encParams = new EncoderParameters(1) {
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
      if (MediaType == MediaTypes.Video) return true;
      var original = new FileInfo(FilePath);
      var newFile = new FileInfo(FilePath + "_newFile");
      var bSuccess = false;
      const BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;

      using (Stream originalFileStream = File.Open(original.FullName, FileMode.Open, FileAccess.Read)) {
        var decoder = BitmapDecoder.Create(originalFileStream, createOptions, BitmapCacheOption.None);
        if (decoder.CodecInfo != null && decoder.CodecInfo.FileExtensions.Contains("jpg") && decoder.Frames[0] != null) {
          var metadata = decoder.Frames[0].Metadata == null
            ? new BitmapMetadata("jpg")
            : decoder.Frames[0].Metadata.Clone() as BitmapMetadata;

          if (metadata != null) {

            //People
            const string microsoftRegionInfo = @"/xmp/MP:RegionInfo";
            const string microsoftRegions = @"/xmp/MP:RegionInfo/MPRI:Regions";
            const string microsoftPersonDisplayName = @"/MPReg:PersonDisplayName";
            var peopleIdx = -1;
            var addedPeople = new List<string>();
            //New metadata just for People
            var people = new BitmapMetadata("jpg");
            people.SetQuery(microsoftRegionInfo, new BitmapMetadata("xmpstruct"));
            people.SetQuery(microsoftRegions, new BitmapMetadata("xmpbag"));
            //Adding existing people
            var existingPeople = metadata.GetQuery(microsoftRegions) as BitmapMetadata;
            if (existingPeople != null) {
              foreach (var idx in existingPeople) {
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
            foreach (var person in People.Where(p => !addedPeople.Any(ap => ap.Equals(p.Title)))) {
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

            //GeoNameId
            if (GeoNameId == null)
              metadata.RemoveQuery(@"/xmp/GeoNames:GeoNameId");
            else
              metadata.SetQuery(@"/xmp/GeoNames:GeoNameId", GeoNameId.ToString());

            var encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };
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

    public void ReadMetadata() {
      try {
        if (MediaType == MediaTypes.Video) {
          var size = ShellStuff.FileInformation.GetVideoDimensions(FilePath);
          Height = size[0];
          Width = size[1];
        }
        else { //MediaTypes.Image
          using (var imageFileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            if (imageFileStream.Length == 0) return;
            var decoder = BitmapDecoder.Create(imageFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
            var frame = decoder.Frames[0];
            Width = frame.PixelWidth;
            Height = frame.PixelHeight;
            var bm = (BitmapMetadata) frame.Metadata;
            if (bm == null) return;

            //People
            People.Clear();
            const string microsoftRegions = @"/xmp/MP:RegionInfo/MPRI:Regions";
            const string microsoftPersonDisplayName = @"/MPReg:PersonDisplayName";

            var regions = bm.GetQuery(microsoftRegions) as BitmapMetadata;
            if (regions != null) {
              foreach (var region in regions) {
                var personDisplayName = bm.GetQuery(microsoftRegions + region + microsoftPersonDisplayName);
                if (personDisplayName != null) {
                  People.Add(ACore.People.GetPerson(personDisplayName.ToString(), true));
                }
              }
            }

            //Rating
            Rating = bm.Rating;

            //Comment
            Comment = bm.Comment == null
              ? string.Empty
              : ACore.IncorectChars.Aggregate(bm.Comment, (current, ch) => current.Replace(ch, string.Empty));

            //Orientation
            var orientation = bm.GetQuery("System.Photo.Orientation");
            if (orientation != null) {
              //3: 180, 6: 270, 8: 90
              Orientation = (ushort) orientation;
            }

            //Keywords
            Keywords.Clear();
            if (bm.Keywords != null) {
              //Filter out duplicities
              foreach (var k in bm.Keywords.OrderByDescending(x => x)) {
                if (Keywords.Where(x => x.FullPath.StartsWith(k)).ToList().Count == 0) {
                  var keyword = ACore.Keywords.GetKeywordByFullPath(k, true);
                  if (keyword != null)
                    Keywords.Add(keyword);
                }
              }
            }

            //GeoNameId
            var tmpGId = bm.GetQuery(@"/xmp/GeoNames:GeoNameId");
            if (tmpGId != null)
              GeoNameId = int.Parse(tmpGId.ToString());

            //Lat Lng
            var tmpLat = bm.GetQuery("System.GPS.Latitude.Proxy")?.ToString();
            if (tmpLat != null) {
              var vals = tmpLat.Substring(0, tmpLat.Length - 1).Split(',');
              Lat = (int.Parse(vals[0]) + double.Parse(vals[1], CultureInfo.InvariantCulture) / 60) *
                    (tmpLat.EndsWith("S") ? -1 : 1);
            }

            var tmpLng = bm.GetQuery("System.GPS.Longitude.Proxy")?.ToString();
            if (tmpLng != null) {
              var vals = tmpLng.Substring(0, tmpLng.Length - 1).Split(',');
              Lng = (int.Parse(vals[0]) + double.Parse(vals[1], CultureInfo.InvariantCulture) / 60) *
                    (tmpLng.EndsWith("W") ? -1 : 1);
            }
          }
        }
      } catch (Exception ex) {
        // ignored
      }
    }

    public void WbUpdateInfo() {
      var thumb = ACore.WbThumbs.Document?.GetElementById(Index.ToString());
      if (thumb == null) return;
      foreach (HtmlElement element in thumb.Children) {
        if (!element.GetAttribute("className").Equals("keywords")) continue;
        element.InnerHtml = GetKeywordsAsString(false);
        break;
      }
    }
  }
}
