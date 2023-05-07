using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.HelperClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

    private void ReadMetadata(MediaItemMetadata mim, bool gpsOnly = false) {
      mim.Success = false;
      try {
        if (mim.MediaItem.MediaType == MediaType.Video) {
          ReadVideoMetadata(mim);
          return;
        }

        if (mim.MediaItem.MediaType == MediaType.Image) {
          using Stream srcFileStream = File.Open(mim.MediaItem.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
          var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
          var frame = decoder.Frames[0];
          
          mim.MediaItem.Width = frame.PixelWidth;
          mim.MediaItem.Height = frame.PixelHeight;

          // true because only media item dimensions are required
          if (frame.Metadata is not BitmapMetadata bm) {
            mim.Success = true;
            return;
          }

          ReadImageMetadata(mim, bm, gpsOnly);
          mim.MediaItem.IsOnlyInDb = false; // TODO
          mim.Success = true;
        }
      }
      catch (Exception ex) {
        Log.Error(ex, mim.MediaItem.FilePath);

        // No imaging component suitable to complete this operation was found.
        if ((ex.InnerException as COMException)?.HResult == -2003292336)
          return;

        mim.MediaItem.IsOnlyInDb = true;

        // true because only media item dimensions are required
        mim.Success = true;
      }
    }

    private void ReadVideoMetadata(MediaItemMetadata mim) {
      try {
        var size = ShellStuff.FileInformation.GetVideoMetadata(mim.MediaItem.Folder.FullPath, mim.MediaItem.FileName);
        mim.MediaItem.Height = (int)size[0];
        mim.MediaItem.Width = (int)size[1];
        mim.MediaItem.Orientation = (int)size[2] switch {
          90 => (int)MediaOrientation.Rotate90,
          180 => (int)MediaOrientation.Rotate180,
          270 => (int)MediaOrientation.Rotate270,
          _ => (int)MediaOrientation.Normal,
        };

        mim.Success = true;
      }
      catch (Exception ex) {
        Log.Error(ex, mim.MediaItem.FilePath);
        mim.Success = false;
      }
    }

    private void ReadImageMetadata(MediaItemMetadata mim, BitmapMetadata bm, bool gpsOnly) {
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
          mim.MediaItem.Lat = (int.Parse(vals[0]) + (double.Parse(vals[1], CultureInfo.InvariantCulture) / 60)) * (tmpLat.EndsWith("S") ? -1 : 1);
        }

        var tmpLng = GetQuery("System.GPS.Longitude.Proxy")?.ToString();
        if (tmpLng != null) {
          var vals = tmpLng[..^1].Split(',');
          mim.MediaItem.Lng = (int.Parse(vals[0]) + (double.Parse(vals[1], CultureInfo.InvariantCulture) / 60)) * (tmpLng.EndsWith("W") ? -1 : 1);
        }

        if (gpsOnly) return;

        // People
        const string microsoftRegions = "/xmp/MP:RegionInfo/MPRI:Regions";
        const string microsoftPersonDisplayName = "/MPReg:PersonDisplayName";

        if (GetQuery(microsoftRegions) is BitmapMetadata regions) {
          mim.People = regions
            .Select(region => GetQuery(microsoftRegions + region + microsoftPersonDisplayName))
            .Where(x => x != null)
            .Select(x => x.ToString())
            .ToArray();
        }

        mim.MediaItem.Rating = bm.Rating;
        mim.MediaItem.Comment = StringUtils.NormalizeComment(bm.Comment);
        // Orientation 1: 0, 3: 180, 6: 270, 8: 90
        mim.MediaItem.Orientation = (ushort?)GetQuery("System.Photo.Orientation") ?? 1;
        mim.Keywords = bm.Keywords?.ToArray();
        mim.GeoName = GetQuery("/xmp/GeoNames:GeoNameId") as string;
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
          Log.Error(ex, mi.FilePath);
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
