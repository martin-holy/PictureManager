using MH.UI.WPF.Extensions;
using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MH.UI.WPF.Utils;

public static class Imaging {
  public static void ResizeJpg(string src, string dest, int px, bool withMetadata, bool withThumbnail, int quality) {
    int GreatestCommonDivisor(int a, int b) {
      while (a != 0 && b != 0) {
        if (a > b)
          a %= b;
        else
          b %= a;
      }

      return a == 0 ? b : a;
    }

    void SetIfContainsQuery(BitmapMetadata bm, string query, object value) {
      if (bm.ContainsQuery(query))
        bm.SetQuery(query, value);
    }

    var srcFile = new FileInfo(src);
    var destFile = new FileInfo(dest);

    using Stream srcFileStream = File.Open(srcFile.FullName, FileMode.Open, FileAccess.Read);
    var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
    if (decoder.CodecInfo?.FileExtensions.Contains("jpg") != true || decoder.Frames[0] == null) return;

    var firstFrame = decoder.Frames[0];

    var pxw = firstFrame.PixelWidth; // image width
    var pxh = firstFrame.PixelHeight; // image height
    var gcd = GreatestCommonDivisor(pxw, pxh);
    var rw = pxw / gcd; // image ratio
    var rh = pxh / gcd; // image ratio
    var q = Math.Sqrt((double)px / (rw * rh)); // Bulgarian constant
    var stw = q * rw / pxw; // scale transform X
    var sth = q * rh / pxh; // scale transform Y

    var resized = new TransformedBitmap(firstFrame, new ScaleTransform(stw, sth, 0, 0));
    var metadata = withMetadata ? firstFrame.Metadata?.Clone() as BitmapMetadata : new BitmapMetadata("jpg");
    var thumbnail = withThumbnail ? firstFrame.Thumbnail : null;

    if (!withMetadata) {
      // even when withMetadata == false, set orientation
      var orientation = ((BitmapMetadata)firstFrame.Metadata)?.GetQuery("System.Photo.Orientation") ?? (ushort)1;
      metadata.SetQuery("System.Photo.Orientation", orientation);
    }

    // ifd ImageWidth a ImageHeight
    SetIfContainsQuery(metadata, "/app1/ifd/{ushort=256}", resized.PixelWidth);
    SetIfContainsQuery(metadata, "/app1/ifd/{ushort=257}", resized.PixelHeight);
    // exif ExifImageWidth a ExifImageHeight
    SetIfContainsQuery(metadata, "/app1/ifd/exif/{ushort=40962}", resized.PixelWidth);
    SetIfContainsQuery(metadata, "/app1/ifd/exif/{ushort=40963}", resized.PixelHeight);

    var encoder = new JpegBitmapEncoder { QualityLevel = quality };
    encoder.Frames.Add(BitmapFrame.Create(resized, thumbnail, metadata, firstFrame.ColorContexts));
    using (Stream destFileStream = File.Open(destFile.FullName, FileMode.Create, FileAccess.ReadWrite))
      encoder.Save(destFileStream);

    // set LastWriteTime to destination file as DateTaken so it can be correctly sorted in mobile apps
    var date = DateTime.MinValue;

    // try to first get dateTaken from file name
    var match = Regex.Match(srcFile.Name, "[0-9]{8}_[0-9]{6}");
    if (match.Success)
      DateTime.TryParseExact(match.Value, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    // try to get dateTaken from metadata
    if (date == DateTime.MinValue) {
      var dateTaken = ((BitmapMetadata)firstFrame.Metadata)?.DateTaken;
      DateTime.TryParse(dateTaken, out date);
    }

    if (date != DateTime.MinValue)
      destFile.LastWriteTime = date;
  }

  public static byte[] GetBitmapHashPixels(string filePath, int bytes) =>
    BitmapSourceExtensions.Create(filePath).GetHashPixels(bytes);
}