using Android.Content;
using Android.Graphics;
using Android.Media;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using PictureManager.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PictureManager.Android.ViewModels;

public static class MediaItemVM {
  public static void ReadMetadata(MediaItemMetadata mim, bool gpsOnly = false) {
    if (mim.MediaItem is VideoM) {
      _readVideoMetadata(mim);
      return;
    }

    try {
      if (mim.MediaItem is ImageM) {
        using System.IO.Stream srcFileStream = File.OpenRead(mim.MediaItem.FilePath);
        var options = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeStream(srcFileStream, null, options);

        mim.Width = options.OutWidth;
        mim.Height = options.OutHeight;

        _readImageMetadata(mim, gpsOnly);
        mim.Success = true;
      }
    }
    catch (Exception ex) {
      Log.Error(ex, mim.MediaItem.FilePath);

      // true because only image dimensions are required
      mim.Success = true;
    }
  }

  private static void _readVideoMetadata(MediaItemMetadata mim) {
    if (ImagingU.GetVideoMetadata(mim.MediaItem.Folder.FullPath, mim.MediaItem.FileName) is not { } data) {
      mim.Success = false;
      Log.Error("Can't read video metadata", mim.MediaItem.FilePath);
      return;
    }

    mim.Height = (int)data[0];
    mim.Width = (int)data[1];
    mim.Orientation = (int)data[2] switch {
      90 => MH.Utils.Imaging.Orientation.Rotate90,
      180 => MH.Utils.Imaging.Orientation.Rotate180,
      270 => MH.Utils.Imaging.Orientation.Rotate270,
      _ => MH.Utils.Imaging.Orientation.Normal,
    };

    mim.Success = true;
  }

  private static void _readImageMetadata(MediaItemMetadata mim, bool gpsOnly = false) {
    var filePath = mim.MediaItem.FilePath;

    using var exif = new ExifInterface(filePath);
    _readExif(mim, exif, gpsOnly);
    if (gpsOnly) return;

    var xmpXml = XmpU.ReadFromJpeg(filePath);
    if (!string.IsNullOrEmpty(xmpXml))
      _readXmpMetadata(mim, xmpXml);
  }

  private static void _readExif(MediaItemMetadata mim, ExifInterface exif, bool gpsOnly) {
    float[] latLng = new float[2];
    if (exif.GetLatLong(latLng)) {
      mim.Lat = latLng[0];
      mim.Lng = latLng[1];
    }

    if (gpsOnly) return;

    var orientationTag = exif.GetAttributeInt(ExifInterface.TagOrientation, (int)global::Android.Media.Orientation.Normal);
    mim.Orientation = ImagingU.ConvertOrientationFromAndroidToMH(orientationTag);

    mim.Comment = StringUtils.NormalizeComment(exif.GetAttribute(ExifInterface.TagUserComment));
  }

  private static void _readXmpMetadata(MediaItemMetadata mim, string xml) {
    try {
      var doc = XDocument.Parse(xml);

      if (int.TryParse(doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "GeoNameId")?.Value, out var geoNameId))
        mim.GeoNameId = geoNameId;

      if (int.TryParse(doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Rating")?.Value, out var rating))
        mim.Rating = rating;

      mim.PeopleSegmentsKeywords = _readPeopleSegmentsKeywords(doc);

      mim.Keywords = doc
        .Descendants()
        .Where(e => e.Name.LocalName == "subject")
        .Descendants()
        .Where(e => e.Name.LocalName == "li")
        .Select(e => e.Value.Trim())
        .Where(v => v.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    }
    catch (Exception ex) {
      Log.Error(ex, "ReadXmpMetadata failed");
    }
  }

  private static List<Tuple<string, List<Tuple<string, string[]?>>>>? _readPeopleSegmentsKeywords(XDocument doc) {
    var regionsBag = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Regions")
      ?.Descendants().FirstOrDefault(e => e.Name.LocalName == "Bag");

    if (regionsBag == null) return null;

    var output = new List<Tuple<string, List<Tuple<string, string[]?>>>>();

    foreach (var li in regionsBag.Elements().Where(e => e.Name.LocalName == "li")) {
      if (li.Elements().FirstOrDefault(e => e.Name.LocalName == "Description") is not { } desc)
        continue;

      var name = desc
        .Descendants()
        .FirstOrDefault(e => e.Name.LocalName == "PersonDisplayName")
        ?.Value?.Trim();

      var rect = desc
        .Descendants()
        .FirstOrDefault(e => e.Name.LocalName == "Rectangle")
        ?.Value?.Trim();

      if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(rect))
        continue;

      var keywords = desc
        .Descendants()
        .Where(e => e.Name.LocalName == "RectangleKeywords")
        .Descendants()
        .Where(e => e.Name.LocalName == "li")
        .Select(e => e.Value.Trim())
        .Where(v => v.Length > 0)
        .ToArray();

      var person = output.FirstOrDefault(x =>
        string.Equals(x.Item1, name, StringComparison.OrdinalIgnoreCase));

      if (person == null) {
        person = new(name, []);
        output.Add(person);
      }

      person.Item2.Add(new(rect, keywords.Length > 0 ? keywords : null));
    }

    return output.Count > 0 ? output : null;
  }

  public static bool WriteMetadata(ImageM img, Context context) {
    var srcPath = img.FilePath;

    try {
      _writeExif(srcPath, img);

      if (_writeXmp(srcPath, img))
        MediaStoreU.ScanFileAsync(context, srcPath);

      return true;
    }
    catch (Exception ex) {
      Log.Error(ex, srcPath);      
      return false;
    }
  }

  private static void _writeExif(string srcPath, ImageM img) {
    var changed = false;
    using var exif = new ExifInterface(srcPath);
    exif.SetUserComment(img.Comment, ref changed);
    exif.SetOrientation(ImagingU.ConvertOrientationFromMHToAndroid(img.Orientation), ref changed);
    exif.SetLatLong(img.GeoLocation?.Lat, img.GeoLocation?.Lng, ref changed);
    if (changed) exif.SaveAttributes();
  }

  private static bool _writeXmp(string srcPath, ImageM img) {
    var existingXmp = XmpU.ReadFromJpeg(srcPath);
    var mergedXmp = ImageS.BuildXmp(existingXmp, img);

    if (string.Equals(existingXmp, mergedXmp, StringComparison.Ordinal))
      return false;

    return XmpU.WriteToJpeg(srcPath, mergedXmp);
  }
}