using Android.Graphics;
using Android.Media;
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
using Orientation = MH.Utils.Imaging.Orientation;

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
      90 => Orientation.Rotate90,
      180 => Orientation.Rotate180,
      270 => Orientation.Rotate270,
      _ => Orientation.Normal,
    };

    mim.Success = true;
  }

  private static void _readImageMetadata(MediaItemMetadata mim, bool gpsOnly = false) {
    var filePath = mim.MediaItem.FilePath;

    using var exif = new ExifInterface(filePath);
    _readExif(mim, exif, gpsOnly);
    if (gpsOnly) return;

    var xmpXml = _readXmpFromJpeg(filePath);
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

    var orientationTag = exif.GetAttributeInt(ExifInterface.TagOrientation, (int)Orientation.Normal);
    mim.Orientation = orientationTag switch {
      (int)Orientation.Rotate90 => Orientation.Rotate90,
      (int)Orientation.Rotate180 => Orientation.Rotate180,
      (int)Orientation.Rotate270 => Orientation.Rotate270,
      _ => Orientation.Normal
    };

    mim.Comment = StringUtils.NormalizeComment(exif.GetAttribute(ExifInterface.TagUserComment));

    if (int.TryParse(exif.GetAttribute("Rating"), out int rating))
      mim.Rating = rating;
  }

  private static string? _readXmpFromJpeg(string path) {
    var xmpHeader = System.Text.Encoding.ASCII.GetBytes("http://ns.adobe.com/xap/1.0/");

    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    using var br = new BinaryReader(fs);

    // Check SOI
    if (fs.Length < 2) return null;
    var soi0 = br.ReadByte();
    var soi1 = br.ReadByte();
    if (soi0 != 0xFF || soi1 != 0xD8) return null; // Not JPEG SOI

    while (fs.Position + 1 < fs.Length) {
      // Find 0xFF
      var b = br.ReadByte();
      while (b != 0xFF && fs.Position < fs.Length)
        b = br.ReadByte();

      if (b != 0xFF) break; // no marker found

      // Skip any padding 0xFF bytes (fill bytes) and read marker
      var marker = br.ReadByte();
      while (marker == 0xFF) marker = br.ReadByte();

      if (marker == 0xD9) break; // EOI
      if (marker == 0xDA) break; // SOS - start of stream for image data; XMP won't be after this

      // Read segment length (big-endian). For markers that have length field.
      if (fs.Position + 2 > fs.Length) break;
      var segLen = _readBigEndianUInt16(br);
      if (segLen < 2) break;
      var payloadLen = segLen - 2;

      if (fs.Position + payloadLen > fs.Length) break;

      if (marker == 0xE1) { // APP1
        var payload = br.ReadBytes(payloadLen);

        // There are two common APP1 structures:
        //  - "Exif\0\0" + TIFF payload  (EXIF)
        //  - "http://ns.adobe.com/xap/1.0/" + XMP packet
        // So check for XMP header anywhere near the start of payload.
        var headerIndex = _indexOf(payload, xmpHeader, 0);
        if (headerIndex >= 0) {
          // XMP often follows header and optionally a null byte.
          var xmlStartIndex = headerIndex + xmpHeader.Length;
          // skip a single 0x00 if present
          if (xmlStartIndex < payload.Length && payload[xmlStartIndex] == 0x00) xmlStartIndex++;

          // We assume the rest of the payload contains the XMP packet text.
          var xmlLen = payload.Length - xmlStartIndex;
          if (xmlLen > 0) {
            // Try UTF8 first, fallback to UTF16 little/big-endian heuristics
            var xml = _tryDecodeXml(payload, xmlStartIndex, xmlLen);
            if (!string.IsNullOrEmpty(xml)) {
              // find xpacket boundaries if present
              var st = xml.IndexOf("<?xpacket", StringComparison.Ordinal);
              var ed = xml.IndexOf("<?xpacket end=\"w\"?>", StringComparison.Ordinal);
              if (st >= 0 && ed > st)
                return xml[st..(ed + "<?xpacket end=\"w\"?>".Length)];

              // some images might not wrap in xpacket, return entire string if it looks like XML
              if (xml.TrimStart().StartsWith('<')) return xml;
            }
          }
        }

        // otherwise continue scanning other segments
      }
      else {
        // not APP1 — skip payload
        fs.Seek(payloadLen, SeekOrigin.Current);
      }
    }

    return null;
  }

  // Try to decode XML payload; handle UTF-8 and UTF-16 BOMs heuristically
  private static string? _tryDecodeXml(byte[] buffer, int offset, int length) {
    if (length <= 0) return null;

    // Check BOM
    if (length >= 3 && buffer[offset] == 0xEF && buffer[offset + 1] == 0xBB && buffer[offset + 2] == 0xBF)
      return System.Text.Encoding.UTF8.GetString(buffer, offset + 3, length - 3);

    if (length >= 2) {
      // UTF-16 LE BOM
      if (buffer[offset] == 0xFF && buffer[offset + 1] == 0xFE)
        return System.Text.Encoding.Unicode.GetString(buffer, offset + 2, length - 2);

      // UTF-16 BE BOM
      if (buffer[offset] == 0xFE && buffer[offset + 1] == 0xFF)
        return System.Text.Encoding.BigEndianUnicode.GetString(buffer, offset + 2, length - 2);
    }

    // Try UTF8 decode, and if it produces a valid-looking XML string, return it.
    try {
      var s = System.Text.Encoding.UTF8.GetString(buffer, offset, length);
      if (s.Contains("<x:xmpmeta") || s.Contains("<rdf:RDF") || s.Contains("<?xpacket") || s.TrimStart().StartsWith("<"))
        return s;
    }
    catch { }

    // fallback: try UTF-16 LE without BOM (some producers might be inconsistent)
    try {
      var s2 = System.Text.Encoding.Unicode.GetString(buffer, offset, length);
      if (s2.Contains("<x:xmpmeta") || s2.Contains("<rdf:RDF") || s2.Contains("<?xpacket"))
        return s2;
    }
    catch { }

    return null;
  }

  private static ushort _readBigEndianUInt16(BinaryReader br) {
    var b0 = br.ReadByte();
    var b1 = br.ReadByte();
    return (ushort)((b0 << 8) | b1);
  }

  private static int _indexOf(byte[] buffer, byte[] pattern, int start) {
    for (int i = start; i <= buffer.Length - pattern.Length; i++) {
      bool ok = true;
      for (int j = 0; j < pattern.Length; j++) {
        if (buffer[i + j] != pattern[j]) {
          ok = false;
          break;
        }
      }
      if (ok) return i;
    }
    return -1;
  }

  private static void _readXmpMetadata(MediaItemMetadata mim, string xml) {
    try {
      var doc = XDocument.Parse(xml);

      if (int.TryParse(doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "GeoNameId")?.Value, out var geoNameId))
        mim.GeoNameId = geoNameId;

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
}