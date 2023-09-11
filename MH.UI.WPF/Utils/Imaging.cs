﻿using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MH.UI.WPF.Utils {
  public static class Imaging {
    public static BitmapSource GetCroppedBitmapSource(string filePath, Int32Rect rect, int size) {
      if (rect.Width == 0 || rect.Height == 0) return null;
      using Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

      var bmp = new BitmapImage();
      bmp.BeginInit();
      bmp.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
      bmp.StreamSource = fileStream;
      bmp.SourceRect = rect;
      bmp.EndInit();
      bmp.Freeze();

      var bmpResized = bmp.Resize(size);
      bmpResized.Freeze();

      return bmpResized;
    }

    public static WriteableBitmap Resize(this BitmapSource bitmapSource, int size) {
      var pxW = bitmapSource.PixelWidth;
      var pxH = bitmapSource.PixelHeight;
      var sizeW = pxW > pxH ? size : size * (pxW / (pxH / 100.0)) / 100;
      var sizeH = pxH > pxW ? size : size * (pxH / (pxW / 100.0)) / 100;
      var scaleX = sizeW / pxW;
      var scaleY = sizeH / pxH;

      return new WriteableBitmap(new TransformedBitmap(bitmapSource, new ScaleTransform(scaleX, scaleY, 0, 0)));
    }

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

    public static void SaveAsJpg(this BitmapSource bitmapSource, int quality, string destFilePath) {
      var encoder = new JpegBitmapEncoder { QualityLevel = quality };
      encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
      using Stream destFileStream = File.Open(destFilePath, FileMode.Create, FileAccess.ReadWrite);
      encoder.Save(destFileStream);
    }
  }
}