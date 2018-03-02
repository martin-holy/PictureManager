using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using PictureManager.Properties;
using Application = System.Windows.Application;

namespace PictureManager.ViewModel {
  public class BaseMediaItem: INotifyPropertyChanged {
    public DataModel.MediaItem Data { get; set; }
    public string FilePath;
    public string FilePathCache => FilePath.Replace(":\\", Settings.Default.CachePath);
    public Uri FilePathUri => new Uri(FilePath);
    public Uri FilePathCacheUri => new Uri(FilePathCache);
    public string CommentEscaped => Data.Comment?.Replace("'", "''");
    public int Index;
    public double? Lat;
    public double? Lng;
    public bool IsModifed;
    public bool IsNew;
    public int ThumbWidth { get; set; }
    public int ThumbHeight { get; set; }
    public List<Keyword> Keywords = new List<Keyword>();
    public List<Person> People = new List<Person>();
    public FolderKeyword FolderKeyword;
    public AppCore ACore;
    public ObservableCollection<string> InfoBoxThumb { get; set; }
    public ObservableCollection<string> InfoBoxFull { get; set; }

    private bool _isSelected;
    public bool IsSelected {
      get => _isSelected;
      set {
        _isSelected = value;
        OnPropertyChanged();
      }
    }

    private MediaTypes _mediaType;
    public MediaTypes MediaType {
      get => _mediaType;
      set {
        _mediaType = value;
        OnPropertyChanged();
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public BaseMediaItem(string filePath, DataModel.MediaItem data, bool isNew = false) {
      InfoBoxThumb = new ObservableCollection<string>();
      InfoBoxFull = new ObservableCollection<string>();
      Data = data;
      ACore = (AppCore) Application.Current.Properties[nameof(AppProps.AppCore)];
      FilePath = filePath;
      IsNew = isNew;
      MediaType = ACore.MediaItems.SuportedImageExts.Any(
        e => Data.FileName.EndsWith(e, StringComparison.InvariantCultureIgnoreCase))
        ? MediaTypes.Image
        : MediaTypes.Video;
      if (!IsNew) SetThumbSize();
    }

    private void SetThumbSize() {
      //windows settings sets everiting to 125%, so I need to lower ratio
      //TODO read the settings from system
      var thumbSize = (int) ((400 / (double) 125) * 100);
      if (Data.Width == 0 && Data.Height == 0) {
        ThumbWidth = thumbSize;
        ThumbHeight = thumbSize;
        return;
      }

      var ratio = Data.Width > Data.Height ? Data.Width / (double) Data.Height : Data.Height / (double) Data.Width;
      
      if (Data.Width > Data.Height) {
        ThumbWidth = thumbSize;
        ThumbHeight = (int) (thumbSize / ratio);
      }
      else {
        ThumbWidth = (int) (thumbSize / ratio);
        ThumbHeight = thumbSize;
      }
    }

    public void SetInfoBox() {
      InfoBoxThumb.Clear();
      InfoBoxFull.Clear();
      InfoBoxFull.Add(FilePath);
      foreach (var p in People.OrderBy(x => x.Title)) {
        InfoBoxThumb.Add(p.Title);
        InfoBoxFull.Add(p.Title);
      }

      var keywordsList = new List<string>();
      foreach (var keyword in Keywords.OrderBy(x => x.Data.Name)) {
        foreach (var k in keyword.Data.Name.Split('/')) {
          if (!keywordsList.Contains(k)) keywordsList.Add(k);
        }
      }

      foreach (var keyword in keywordsList) {
        InfoBoxThumb.Add(keyword);
        InfoBoxFull.Add(keyword);
      }

      InfoBoxThumb.Add(Data.Rating.ToString());
      InfoBoxFull.Add(Data.Rating.ToString());
      if (Data.Comment != string.Empty) InfoBoxThumb.Add("C");
      if (Data.GeoNameId != null) InfoBoxThumb.Add("G");
      if (MediaType == MediaTypes.Video) InfoBoxThumb.Add("V");
    }

    public void SaveMediaItemInToDb(bool update, List<DataModel.BaseTable>[] lists) {
      if (IsNew) {
        ReadMetadata();
        ACore.Db.InsertOnSubmit(Data, lists);
        IsNew = false;
      } else {
        if (update) ReadMetadata();
        ACore.Db.UpdateOnSubmit(Data, lists);
      }

      SaveMediaItemKeywordsToDb(lists);
      SaveMediaItemPeopleInToDb(lists);
    }

    public void SaveMediaItemKeywordsToDb(List<DataModel.BaseTable>[] lists) {
      //Update connection between Keywords and MediaItem
      var keyIds = Keywords.Select(k => k.Data.Id).ToList();
      foreach (var mik in ACore.Db.MediaItemKeywords.Where(x => x.MediaItemId == Data.Id)) {
        if (Keywords.FirstOrDefault(x => x.Data.Id == mik.KeywordId) == null)
          ACore.Db.DeleteOnSubmit(mik, lists);
        else
          keyIds.Remove(mik.KeywordId);
      }
      //Insert new Keywords to MediaItem
      foreach (var keyId in keyIds) {
        ACore.Db.InsertOnSubmit(new DataModel.MediaItemKeyword {
          Id = ACore.Db.GetNextIdFor<DataModel.MediaItemKeyword>(),
          KeywordId = keyId,
          MediaItemId = Data.Id
        }, lists);
      }
    }

    public void SaveMediaItemPeopleInToDb(List<DataModel.BaseTable>[] lists) {
      //Update connection between People and MediaItem
      var ids = People.Select(p => p.Data.Id).ToList();
      foreach (var mip in ACore.Db.MediaItemPeople.Where(x => x.MediaItemId == Data.Id)) {
        if (People.FirstOrDefault(x => x.Data.Id == mip.PersonId) == null)
          ACore.Db.DeleteOnSubmit(mip, lists);
         else
          ids.Remove(mip.PersonId);
      }
      //Insert new People to MediaItem
      foreach (var id in ids) {
        ACore.Db.InsertOnSubmit(new DataModel.MediaItemPerson {
          Id = ACore.Db.GetNextIdFor<DataModel.MediaItemPerson>(),
          PersonId = id,
          MediaItemId = Data.Id
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
      if (WriteMetadata()) return true;
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
            if (metadata.GetQuery(microsoftRegions) is BitmapMetadata existingPeople) {
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


            metadata.Rating = Data.Rating;
            metadata.Comment = Data.Comment ?? string.Empty;
            metadata.Keywords = new ReadOnlyCollection<string>(Keywords.Select(k => k.Data.Name).ToList());

            //GeoNameId
            if (Data.GeoNameId == null)
              metadata.RemoveQuery(@"/xmp/GeoNames:GeoNameId");
            else
              metadata.SetQuery(@"/xmp/GeoNames:GeoNameId", Data.GeoNameId.ToString());

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

    public bool ReadMetadata(bool gpsOnly = false) {
      try {
        if (MediaType == MediaTypes.Video) {
          var size = ShellStuff.FileInformation.GetVideoDimensions(FilePath);
          Data.Height = size[0];
          Data.Width = size[1];
        }
        else { //MediaTypes.Image
          using (var imageFileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            if (imageFileStream.Length == 0) return false;
            var decoder = BitmapDecoder.Create(imageFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
            var frame = decoder.Frames[0];
            Data.Width = frame.PixelWidth;
            Data.Height = frame.PixelHeight;
            var bm = (BitmapMetadata) frame.Metadata;
            if (bm == null) return false;

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

            if (gpsOnly) return true;

            //People
            People.Clear();
            const string microsoftRegions = @"/xmp/MP:RegionInfo/MPRI:Regions";
            const string microsoftPersonDisplayName = @"/MPReg:PersonDisplayName";

            if (bm.GetQuery(microsoftRegions) is BitmapMetadata regions) {
              foreach (var region in regions) {
                var personDisplayName = bm.GetQuery(microsoftRegions + region + microsoftPersonDisplayName);
                if (personDisplayName != null) {
                  People.Add(ACore.People.GetPerson(personDisplayName.ToString(), true));
                }
              }
            }

            //Rating
              Data.Rating = bm.Rating;

            //Comment
            Data.Comment = bm.Comment == null
              ? string.Empty
              : ACore.IncorectChars.Aggregate(bm.Comment, (current, ch) => current.Replace(ch, string.Empty));

            //Orientation
            var orientation = bm.GetQuery("System.Photo.Orientation");
            if (orientation != null) {
              //3: 180, 6: 270, 8: 90
              Data.Orientation = (ushort) orientation;
            }

            //Keywords
            Keywords.Clear();
            if (bm.Keywords != null) {
              //Filter out duplicities
              foreach (var k in bm.Keywords.OrderByDescending(x => x)) {
                if (Keywords.Where(x => x.Data.Name.StartsWith(k)).ToList().Count != 0) continue;
                var keyword = ACore.Keywords.GetKeywordByFullPath(k, true);
                if (keyword != null)
                  Keywords.Add(keyword);
              }
            }

            //GeoNameId
            var tmpGId = bm.GetQuery(@"/xmp/GeoNames:GeoNameId");
            if (tmpGId != null)
              Data.GeoNameId = int.Parse(tmpGId.ToString());
          }
        }

        SetThumbSize();
      } catch {
        return false;
      }
      return true;
    }
  }
}
