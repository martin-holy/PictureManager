using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PictureManager.ViewModels {
  public class MediaItemsVM : ObservableObject {
    private readonly Core _core;

    public MediaItemsM Model { get; }

    public MediaItemsVM(Core core, MediaItemsM model) {
      _core = core;
      Model = model;

      Model.ReadMetadata = ReadMetadata;
      Model.WriteMetadata = WriteMetadata;
    }

    private async Task<bool> ReadMetadata(MediaItemM mi, bool gpsOnly = false) {
      try {
        if (mi.MediaType == MediaType.Video) {
          await Core.RunOnUiThread(() => ReadVideoMetadata(mi));
        }
        else {
          await using Stream srcFileStream = File.Open(mi.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
          var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
          var frame = decoder.Frames[0];
          mi.Width = frame.PixelWidth;
          mi.Height = frame.PixelHeight;
          mi.SetThumbSize(true);

          if (!mi.IsNew)
            Model.DataAdapter.IsModified = true;

          // true because only media item dimensions are required
          if (frame.Metadata is not BitmapMetadata bm) return true;

          await ReadImageMetadata(mi, bm, gpsOnly);

          mi.SetThumbSize(true);
        }

        mi.IsOnlyInDb = false;
      }
      catch (Exception ex) {
        _core.LogError(ex, mi.FilePath);

        // No imaging component suitable to complete this operation was found.
        if ((ex.InnerException as COMException)?.HResult == -2003292336)
          return false;

        mi.IsOnlyInDb = true;

        // true because only media item dimensions are required
        return true;
      }

      return true;
    }

    private void ReadVideoMetadata(MediaItemM mi) {
      try {
        var size = ShellStuff.FileInformation.GetVideoMetadata(mi.Folder.FullPath, mi.FileName);
        mi.Height = (int)size[0];
        mi.Width = (int)size[1];
        mi.Orientation = (int)size[2] switch {
          90 => (int)MediaOrientation.Rotate90,
          180 => (int)MediaOrientation.Rotate180,
          270 => (int)MediaOrientation.Rotate270,
          _ => (int)MediaOrientation.Normal,
        };
        mi.SetThumbSize(true);

        Model.DataAdapter.IsModified = true;
      }
      catch (Exception ex) {
        _core.LogError(ex, mi.FilePath);
      }
    }

    private async Task ReadImageMetadata(MediaItemM mi, BitmapMetadata bm, bool gpsOnly) {
      object GetQuery(string query) {
        try {
          return bm.GetQuery(query);
        }
        catch (Exception) {
          return null;
        }
      }

      try {
        // Lat Lng
        var tmpLat = GetQuery("System.GPS.Latitude.Proxy")?.ToString();
        if (tmpLat != null) {
          var vals = tmpLat[..^1].Split(',');
          mi.Lat = (int.Parse(vals[0]) + (double.Parse(vals[1], CultureInfo.InvariantCulture) / 60)) * (tmpLat.EndsWith("S") ? -1 : 1);
        }

        var tmpLng = GetQuery("System.GPS.Longitude.Proxy")?.ToString();
        if (tmpLng != null) {
          var vals = tmpLng[..^1].Split(',');
          mi.Lng = (int.Parse(vals[0]) + (double.Parse(vals[1], CultureInfo.InvariantCulture) / 60)) * (tmpLng.EndsWith("W") ? -1 : 1);
        }

        if (gpsOnly) return;

        // People
        mi.People = null;
        const string microsoftRegions = "/xmp/MP:RegionInfo/MPRI:Regions";
        const string microsoftPersonDisplayName = "/MPReg:PersonDisplayName";

        if (GetQuery(microsoftRegions) is BitmapMetadata regions) {
          var people = regions
            .Select(region => GetQuery(microsoftRegions + region + microsoftPersonDisplayName))
            .Where(x => x != null)
            .ToArray();

          if (people.Any())
            await Core.RunOnUiThread(() => {
              mi.People = new(people.Length);
              foreach (var person in people)
                mi.People.Add(_core.PeopleM.GetPerson(person.ToString(), true));
            });
        }

        // Rating
        mi.Rating = bm.Rating;    

        // Comment
        mi.Comment = StringUtils.NormalizeComment(bm.Comment);

        // Orientation 1: 0, 3: 180, 6: 270, 8: 90
        mi.Orientation = (ushort?)GetQuery("System.Photo.Orientation") ?? 1;

        // Keywords
        mi.Keywords = null;
        if (bm.Keywords != null) {
          await Core.RunOnUiThread(() => {
            mi.Keywords = new();
            foreach (var k in bm.Keywords.OrderByDescending(x => x).Distinct()) {
              var keyword = _core.KeywordsM.GetByFullPath(k.Replace('|', ' '));
              if (keyword != null)
                mi.Keywords.Add(keyword);
            }
          });
        }

        // GeoNameId
        var tmpGId = GetQuery("/xmp/GeoNames:GeoNameId") as string;
        // TODO change condition
        if (!string.IsNullOrEmpty(tmpGId)) {
          // TODO find/create GeoName
          mi.GeoName = _core.GeoNamesM.DataAdapter.All.Values.SingleOrDefault(x => x.Id == int.Parse(tmpGId));
        }
      }
      catch (Exception) {
        // ignored
      }
    }

    private static bool WriteMetadata(MediaItemM mi) {
      if (mi.MediaType == MediaType.Video) return true;
      var original = new FileInfo(mi.FilePath);
      var newFile = new FileInfo(mi.FilePath + "_newFile");
      var bSuccess = false;
      const BitmapCreateOptions createOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;

      using (Stream originalFileStream = File.Open(original.FullName, FileMode.Open, FileAccess.Read)) {
        var decoder = BitmapDecoder.Create(originalFileStream, createOptions, BitmapCacheOption.None);
        if (decoder.CodecInfo?.FileExtensions.Contains("jpg") == true && decoder.Frames[0] != null) {
          var metadata = decoder.Frames[0].Metadata == null
            ? new("jpg")
            : decoder.Frames[0].Metadata.Clone() as BitmapMetadata;

          if (metadata != null) {
            //People
            if (mi.People != null) {
              const string microsoftRegionInfo = "/xmp/MP:RegionInfo";
              const string microsoftRegions = "/xmp/MP:RegionInfo/MPRI:Regions";
              const string microsoftPersonDisplayName = "/MPReg:PersonDisplayName";
              var peopleIdx = -1;
              var addedPeople = new List<string>();
              //New metadata just for People
              var people = new BitmapMetadata("jpg");
              people.SetQuery(microsoftRegionInfo, new BitmapMetadata("xmpstruct"));
              people.SetQuery(microsoftRegions, new BitmapMetadata("xmpbag"));
              //Adding existing people => preserve original metadata because they can contain positions of people
              if (metadata.GetQuery(microsoftRegions) is BitmapMetadata existingPeople) {
                foreach (var idx in existingPeople) {
                  var existingPerson = metadata.GetQuery(microsoftRegions + idx) as BitmapMetadata;
                  var personDisplayName = existingPerson?.GetQuery(microsoftPersonDisplayName);
                  if (personDisplayName == null) continue;
                  if (!mi.People.Any(p => p.Name.Equals(personDisplayName.ToString()))) continue;
                  addedPeople.Add(personDisplayName.ToString());
                  peopleIdx++;
                  people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}", existingPerson);
                }
              }

              //Adding new people
              foreach (var person in mi.People.Where(p => !addedPeople.Any(ap => ap.Equals(p.Name)))) {
                peopleIdx++;
                people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}", new BitmapMetadata("xmpstruct"));
                people.SetQuery($"{microsoftRegions}/{{ulong={peopleIdx}}}" + microsoftPersonDisplayName, person.Name);
              }

              //Writing all people to MediaItem metadata
              var allPeople = people.GetQuery(microsoftRegionInfo);
              if (allPeople != null)
                metadata.SetQuery(microsoftRegionInfo, allPeople);
            }

            metadata.Rating = mi.Rating;
            metadata.Comment = mi.Comment ?? string.Empty;
            metadata.Keywords = new(mi.Keywords?.Select(k => k.FullName).ToList() ?? new List<string>());
            metadata.SetQuery("System.Photo.Orientation", (ushort)mi.Orientation);

            //GeoNameId
            if (mi.GeoName == null)
              metadata.RemoveQuery("/xmp/GeoNames:GeoNameId");
            else
              metadata.SetQuery("/xmp/GeoNames:GeoNameId", mi.GeoName.Id.ToString());

            bSuccess = WriteMetadataToFile(mi, newFile, decoder, metadata, true);
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

    private static bool WriteMetadataToFile(MediaItemM mi, FileInfo newFile, BitmapDecoder decoder, BitmapMetadata metadata, bool withThumbnail) {
      bool bSuccess;
      var hResult = 0;
      var encoder = new JpegBitmapEncoder { QualityLevel = Core.Settings.JpegQualityLevel };
      encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0],
        withThumbnail ? decoder.Frames[0].Thumbnail : null, metadata, decoder.Frames[0].ColorContexts));

      try {
        using Stream newFileStream = File.Open(newFile.FullName, FileMode.Create, FileAccess.ReadWrite);
        encoder.Save(newFileStream);

        bSuccess = true;
      }
      catch (Exception ex) {
        bSuccess = false;
        hResult = ex.HResult;

        // don't log error if hResult is -2146233033
        if (hResult != -2146233033)
          Core.Instance.LogError(ex, mi.FilePath);
      }

      // There is too much metadata to be written to the bitmap. (Exception from HRESULT: 0x88982F52)
      // It might be problem with ThumbnailImage in JPEG images taken by Huawei P10
      if (!bSuccess && hResult == -2146233033) {
        if (metadata.ContainsQuery("/app1/thumb/"))
          metadata.RemoveQuery("/app1/thumb/");
        else {
          // removing thumbnail wasn't enough
          return false;
        }

        bSuccess = WriteMetadataToFile(mi, newFile, decoder, metadata, false);
      }

      return bSuccess;
    }
  }
}
