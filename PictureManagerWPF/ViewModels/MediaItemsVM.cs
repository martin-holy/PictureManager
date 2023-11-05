using MH.Utils;
using MH.Utils.Extensions;
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

namespace PictureManager.ViewModels;

public static class MediaItemsVM {
  private const string _msRegionInfo = "/xmp/MP:RegionInfo";
  private const string _msRegions = "/xmp/MP:RegionInfo/MPRI:Regions";
  private const string _msPersonName = "/MPReg:PersonDisplayName";
  private const string _msPersonRectangle = "/MPReg:Rectangle";
  private const string _msPersonRectangleKeywords = "/MPReg:RectangleKeywords";

  public static void ReadMetadata(MediaItemMetadata mim, bool gpsOnly = false) {
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
        mim.Success = true;
      }
    }
    catch (Exception ex) {
      Log.Error(ex, mim.MediaItem.FilePath);

      // No imaging component suitable to complete this operation was found.
      if ((ex.InnerException as COMException)?.HResult == -2003292336)
        return;

      // true because only media item dimensions are required
      mim.Success = true;
    }
  }

  private static void ReadVideoMetadata(MediaItemMetadata mim) {
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

  private static void ReadImageMetadata(MediaItemMetadata mim, BitmapMetadata bm, bool gpsOnly) {
    try {
      mim.MediaItem.Lat = GetGps(bm, "System.GPS.Latitude.Proxy");
      mim.MediaItem.Lng = GetGps(bm, "System.GPS.Longitude.Proxy");
      if (gpsOnly) return;

      mim.PeopleSegmentsKeywords = ReadPeopleSegmentsKeywords(bm);
      mim.MediaItem.Rating = bm.Rating;
      mim.MediaItem.Comment = StringUtils.NormalizeComment(bm.Comment);
      // Orientation 1: 0, 3: 180, 6: 270, 8: 90
      mim.MediaItem.Orientation = (ushort?)TryGetQuery(bm, "System.Photo.Orientation") ?? 1;
      mim.Keywords = bm.Keywords?.ToArray();
      mim.GeoName = TryGetQuery(bm, "/xmp/GeoNames:GeoNameId")?.ToString();
    }
    catch (Exception) {
      // ignored
    }
  }

  private static double? GetGps(BitmapMetadata bm, string query) {
    var val = TryGetQuery(bm, query)?.ToString();
    if (val == null) return null;
    var vals = val[..^1].Split(',');

    return (int.Parse(vals[0]) + double.Parse(vals[1], CultureInfo.InvariantCulture) / 60)
           * (val.EndsWith("S") || val.EndsWith("W") ? -1 : 1);
  }

  private static List<Tuple<string, List<Tuple<string, string[]>>>> ReadPeopleSegmentsKeywords(BitmapMetadata bm) {
    if (TryGetQuery(bm, _msRegions) is not BitmapMetadata regions) return null;
    var output = new List<Tuple<string, List<Tuple<string, string[]>>>>();
      
    foreach (var r in regions.Select(x => _msRegions + x)) {
      if (TryGetQuery(bm, r + _msPersonName)?.ToString() is not { } name) continue;
        
      if (output.SingleOrDefault(x => x.Item1.Equals(name, StringComparison.OrdinalIgnoreCase)) is not { } person) {
        person = new(name, new());
        output.Add(person);
      }

      if (TryGetQuery(bm, r + _msPersonRectangle)?.ToString() is not { } rect) continue;

      var keywords = TryGetQuery(bm, r + _msPersonRectangleKeywords) as BitmapMetadata;
      person.Item2.Add(new(rect, keywords?
        .Select(x => TryGetQuery(keywords, x)?.ToString())
        .Where(x => x != null)
        .ToArray()));
    }

    return output;
  }

  public static bool WriteMetadata(MediaItemM mi) {
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
          WritePeople(metadata, mi);
          metadata.Rating = mi.Rating;
          metadata.Comment = mi.Comment ?? string.Empty;
          metadata.Keywords = new(mi.Keywords?.Select(k => k.FullName).ToList() ?? new List<string>());
          metadata.SetQuery("System.Photo.Orientation", (ushort)mi.Orientation);
          SetOrRemoveQuery(metadata, "/xmp/GeoNames:GeoNameId", mi.GeoName?.Id.ToString());

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

  private static void WritePeople(BitmapMetadata bm, MediaItemM mi) {
    var peopleRects = GetPeopleSegmentsKeywords(mi);
    if (peopleRects == null) {
      if (bm.ContainsQuery(_msRegionInfo))
        bm.RemoveQuery(_msRegionInfo);

      return;
    }

    var regIdx = -1;
    var regions = new BitmapMetadata("jpg");
    regions.SetQuery(_msRegionInfo, new BitmapMetadata("xmpstruct"));
    regions.SetQuery(_msRegions, new BitmapMetadata("xmpbag"));

    // preserve original metadata because they can contain additional data
    if (TryGetQuery(bm, _msRegions) is BitmapMetadata existingRegions) {
      foreach (var idx in existingRegions) {
        if (TryGetQuery(bm, _msRegions + idx) as BitmapMetadata is not { } reg) continue;
        if (TryGetQuery(reg, _msPersonName)?.ToString() is not { } name) continue;
        if (peopleRects.FirstOrDefault(x => name.Equals(x.Item1?.Name, StringComparison.OrdinalIgnoreCase)) is not { } pr) continue;
        peopleRects.Remove(pr);

        SetOrRemoveQuery(reg, _msPersonRectangle, pr.Item2);
        WritePersonRectangleKeywords(reg, _msPersonRectangleKeywords, pr.Item3);

        regIdx++;
        regions.SetQuery($"{_msRegions}/{{ulong={regIdx}}}", reg);
      }
    }

    // add rest of peopleRects
    foreach (var pr in peopleRects) {
      regIdx++;
      var r = $"{_msRegions}/{{ulong={regIdx}}}";
      regions.SetQuery(r, new BitmapMetadata("xmpstruct"));
      SetOrRemoveQuery(regions, r + _msPersonName, pr.Item1?.Name);
      SetOrRemoveQuery(regions, r + _msPersonRectangle, pr.Item2);
      WritePersonRectangleKeywords(regions, r + _msPersonRectangleKeywords, pr.Item3);
    }

    if (TryGetQuery(regions, _msRegionInfo) is { } ri)
      bm.SetQuery(_msRegionInfo, ri);
  }

  private static List<Tuple<PersonM, string, string[]>> GetPeopleSegmentsKeywords(MediaItemM mi) {
    var peopleOnSegments = mi.Segments.EmptyIfNull().Select(x => x.Person).Distinct().ToHashSet();

    return mi.Segments?
      .Select(x => new Tuple<PersonM, string, string[]>(
        x.Person,
        x.ToMsRect(),
        x.Keywords?.Select(k => k.FullName).ToArray()))
      .Concat(mi.People
        .EmptyIfNull()
        .Where(x => !peopleOnSegments.Contains(x))
        .Select(x => new Tuple<PersonM, string, string[]>(x, null, null)))
      .ToList();
  }

  private static void WritePersonRectangleKeywords(BitmapMetadata bm, string query, string[] keywords) {
    if (keywords != null) {
      var idx = -1;
      bm.SetQuery(query, new BitmapMetadata("xmpbag"));
      foreach (var keyword in keywords) {
        idx++;
        bm.SetQuery($"{query}/{{ulong={idx}}}", keyword);
      }
    }
    else if (bm.ContainsQuery(query))
      bm.RemoveQuery(query);
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

  private static object TryGetQuery(BitmapMetadata bm, string query) {
    try {
      return bm.GetQuery(query);
    }
    catch (Exception) {
      return null;
    }
  }

  private static void SetOrRemoveQuery(BitmapMetadata bm, string query, object value) {
    if (value != null)
      bm.SetQuery(query, value);
    else if (bm.ContainsQuery(query))
      bm.RemoveQuery(query);
  }
}