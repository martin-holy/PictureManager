using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using PictureManager.Properties;
using Application = System.Windows.Application;

namespace PictureManager.Database {
  public class BaseMediaItem : INotifyPropertyChanged, IRecord {
    public string[] Csv { get; set; }

    // DB Fields
    public int Id { get; set; }
    public Folder Folder { get; set; }
    public string FileName { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Orientation { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; }
    public GeoName GeoName { get; set; }
    public List<Person> People { get; } = new List<Person>();
    public List<Keyword> Keywords { get; } = new List<Keyword>();

    private bool _isSelected;
    private int _thumbWidth;
    private int _thumbHeight;
    private MediaType _mediaType;

    public int ThumbWidth { get => _thumbWidth; set { _thumbWidth = value; OnPropertyChanged(); } }
    public int ThumbHeight { get => _thumbHeight; set { _thumbHeight = value; OnPropertyChanged(); } }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public MediaType MediaType { get => _mediaType; set { _mediaType = value; OnPropertyChanged(); } }
    public ObservableCollection<string> InfoBoxThumb { get; set; } = new ObservableCollection<string>();
    public ObservableCollection<string> InfoBoxPeople { get; set; } = new ObservableCollection<string>();
    public ObservableCollection<string> InfoBoxKeywords { get; set; } = new ObservableCollection<string>();

    public string FilePath => Extensions.PathCombine(Folder.FullPath, FileName);
    public string FilePathCache => FilePath.Replace(Path.VolumeSeparatorChar.ToString(), Settings.Default.CachePath);
    public Uri FilePathUri => new Uri(FilePath);
    public Uri FilePathCacheUri => new Uri(FilePathCache);
    public string CommentEscaped => Comment?.Replace("'", "''") ?? string.Empty;
    public int Index { get; set; }
    public int ThumbSize { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public bool IsModifed { get; set; }
    public bool IsNew { get; set; }
    public bool IsPanoramatic { get; set; }

    public AppCore ACore => (AppCore) Application.Current.Properties[nameof(AppProperty.AppCore)];

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public BaseMediaItem(int id, Folder folder, string fileName, bool isNew = false) {
      Id = id;
      Folder = folder;
      FileName = fileName;
      IsNew = isNew;
      MediaType = MediaItems.SuportedImageExts.Any(
        x => FileName.EndsWith(x, StringComparison.InvariantCultureIgnoreCase))
        ? MediaType.Image
        : MediaType.Video;
    }

    public string ToCsv() {
      // ID|Folder|Name|Width|Height|Orientation|Rating|Comment|GeoName|People|Keywords
      return string.Join("|",
        Id.ToString(),
        Folder.Id.ToString(),
        FileName,
        Width.ToString(),
        Height.ToString(),
        Orientation.ToString(),
        Rating.ToString(),
        Comment,
        GeoName?.Id.ToString(),
        string.Join(",", People.Select(x => x.Id)),
        string.Join(",", Keywords.Select(x => x.Id)));
    }

    public void SetThumbSize() {
      var size = GetThumbSize();
      ThumbWidth = (int)size.Width;
      ThumbHeight = (int)size.Height;
      ThumbSize = (int)((ThumbWidth > ThumbHeight ? ThumbWidth : ThumbHeight) * ACore.WindowsDisplayScale / 100 / ACore.ThumbScale);
    }

    private Size GetThumbSize() {
      var size = new Size();
      var desiredSize = Settings.Default.ThumbnailSize / ACore.WindowsDisplayScale * 100 * ACore.ThumbScale;

      if (Width == 0 || Height == 0) {
        size.Width = desiredSize;
        size.Height = desiredSize;
        return size;
      }

      var rotated = Orientation == (int)MediaOrientation.Rotate90 ||
                    Orientation == (int)MediaOrientation.Rotate270;
      var width = (double)(rotated ? Height : Width);
      var height = (double)(rotated ? Width : Height);


      if (width > height) {
        //panorama
        if (width / height > 16.0 / 9.0) {
          IsPanoramatic = true;
          const int maxWidth = 1100;
          var panoramaHeight = desiredSize / 16.0 * 9;
          var tooBig = panoramaHeight / height * width > maxWidth;
          size.Height = tooBig ? maxWidth / width * height : panoramaHeight;
          size.Width = tooBig ? maxWidth : panoramaHeight / height * width;
          return size;
        }

        size.Height = desiredSize / width * height;
        size.Width = desiredSize;
        return size;
      }

      size.Height = desiredSize;
      size.Width = desiredSize / height * width;
      return size;
    }

    public void SetInfoBox() {
      InfoBoxThumb.Clear();
      InfoBoxPeople.Clear();
      InfoBoxKeywords.Clear();

      foreach (var p in People.OrderBy(x => x.Title))
        InfoBoxPeople.Add(p.Title);

      foreach (var keyword in Keywords) {
        foreach (var k in keyword.FullPath.Split('/'))
          if (!InfoBoxKeywords.Contains(k))
            InfoBoxKeywords.Add(k);
      }

      if (Rating != 0)
        InfoBoxThumb.Add(Rating.ToString());

      if (!string.IsNullOrEmpty(Comment))
        InfoBoxThumb.Add(Comment);

      if (GeoName != null)
        InfoBoxThumb.Add(GeoName.Title);

      foreach (var val in InfoBoxPeople)
        InfoBoxThumb.Add(val);

      foreach (var val in InfoBoxKeywords)
        InfoBoxThumb.Add(val);
    }

    public BaseMediaItem CopyTo(Folder folder, string fileName) {
      var copy = (BaseMediaItem)MemberwiseClone();
      copy.Id = ACore.MediaItems.Helper.GetNextId();
      copy.FileName = fileName;
      copy.Folder = folder;
      copy.Folder.MediaItems.Add(copy);
      copy.GeoName?.MediaItems.Add(copy);
      copy.People.ForEach(x => x.MediaItems.Add(copy));
      copy.Keywords.ForEach(x => x.MediaItems.Add(copy));

      ACore.MediaItems.AddRecord(copy);
      ACore.AppInfo.MediaItemsCount++;

      return copy;
    }

    public void MoveTo(Folder folder, string fileName) {
      // delete existing MediaItem if exists
      ACore.MediaItems.Delete(folder.MediaItems.SingleOrDefault(x => x.FileName.Equals(fileName)));

      FileName = fileName;
      Folder.MediaItems.Remove(this);
      Folder = folder;
      Folder.MediaItems.Add(this);
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
                Param = { [0] = new EncoderParameter(Encoder.Quality, Settings.Default.JpegQualityLevel) }
              };
              bmp.Save(newFileStream, encoder, encParams);
            }
          }
        }

        newFile.CreationTime = original.CreationTime;
        original.Delete();
        newFile.MoveTo(original.FullName);
      }
      catch (Exception ex) {
        if (newFile.Exists) newFile.Delete();
        AppCore.ShowErrorDialog(ex);
      }
    }

    public bool TryWriteMetadata() {
      ACore.MediaItems.Helper.IsModifed = true;
      if (WriteMetadata()) return true;
      ReSave();
      return WriteMetadata();
    }

    public bool WriteMetadata() {
      if (MediaType == MediaType.Video) return true;
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


            metadata.Rating = Rating;
            metadata.Comment = Comment ?? string.Empty;
            metadata.Keywords = new ReadOnlyCollection<string>(Keywords.Select(k => k.Title).ToList());

            //GeoNameId
            if (GeoName == null)
              metadata.RemoveQuery(@"/xmp/GeoNames:GeoNameId");
            else
              metadata.SetQuery(@"/xmp/GeoNames:GeoNameId", GeoName.Id.ToString());

            var encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };
            encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0], decoder.Frames[0].Thumbnail, metadata,
              decoder.Frames[0].ColorContexts));

            var hResult = 0;
            try {
              using (Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
                encoder.Save(newFileStream);
              }
              bSuccess = true;
            }
            catch (Exception ex) {
              hResult = ex.HResult;
              if (hResult != -2146233033)
                MessageBox.Show(ex.Message);
              bSuccess = false;
            }

            //There is too much metadata to be written to the bitmap. (Exception from HRESULT: 0x88982F52)
            //Problem with ThumbnailImage in JPEG images taken by Huawei P10
            if (!bSuccess && hResult == -2146233033) {
              if (metadata.ContainsQuery("/app1/thumb/"))
                metadata.RemoveQuery("/app1/thumb/");
              encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };
              encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0], null, metadata,
                decoder.Frames[0].ColorContexts));

              try {
                using (Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
                  encoder.Save(newFileStream);
                }

                bSuccess = true;
              }
              catch (Exception ex) {
                bSuccess = false;
                AppCore.ShowErrorDialog(ex);
              }
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
        if (MediaType == MediaType.Video) {
          Application.Current.Dispatcher.Invoke(delegate {
            try {
              var size = ShellStuff.FileInformation.GetVideoMetadata(Folder.FullPath, FileName);
              Height = size[0];
              Width = size[1];
              Orientation = size[2];
            }
            catch (Exception ex) {
              AppCore.ShowErrorDialog(ex);
            }
          });
        }
        else {
          var decoder = BitmapDecoder.Create(new Uri(FilePath), BitmapCreateOptions.None, BitmapCacheOption.None);
          var frame = decoder.Frames[0];
          Width = frame.PixelWidth;
          Height = frame.PixelHeight;
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
          Rating = bm.Rating;

          //Comment
          Comment = bm.Comment == null
            ? string.Empty
            : new string(bm.Comment.Where(char.IsLetterOrDigit).ToArray());

          //Orientation 1: 0, 3: 180, 6: 270, 8: 90
          var orientation = bm.GetQuery("System.Photo.Orientation") ?? (ushort) 1;
          Orientation = (ushort) orientation;

          //Keywords
          Keywords.Clear();
          if (bm.Keywords != null) {
            //Filter out duplicities
            foreach (var k in bm.Keywords.OrderByDescending(x => x)) {
              if (Keywords.SingleOrDefault(x => x.FullPath.Equals(k)) != null) continue;
              var keyword = ACore.Keywords.GetByFullPath(k);
              if (keyword != null)
                Keywords.Add(keyword);
            }
          }

          //GeoNameId
          var tmpGId = bm.GetQuery(@"/xmp/GeoNames:GeoNameId");
          if (!string.IsNullOrEmpty(tmpGId as string)) {
            // TODO dohledani/vytvoreni geoname
            ACore.GeoNames.AllDic.TryGetValue(int.Parse(tmpGId.ToString()), out var geoname);
            GeoName = geoname;
          }
        }

        SetThumbSize();

        ACore.MediaItems.Helper.IsModifed = true;
      }
      catch (Exception ex) {
        AppCore.ShowErrorDialog(ex, FilePath);
        return false;
      }
      return true;
    }
  }
}
