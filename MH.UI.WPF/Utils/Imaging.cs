using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MH.UI.WPF.Utils {
  public static class Imaging {
    public static FormatConvertedBitmap ToGray(this BitmapSource bitmapSource) =>
      new(bitmapSource, PixelFormats.Gray8, BitmapPalettes.Gray256, 0.0);

    public static CroppedBitmap Cropp(this BitmapSource bitmapSource, Int32Rect sourceRect) =>
      new(bitmapSource, sourceRect);

    public static Task<int[]> GetImageDimensionsAsync(string filePath) {
      return Task.Run(() => {
        try {
          using Stream srcFileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
          var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
          var frame = decoder.Frames[0];
          return new[] { frame.PixelWidth, frame.PixelHeight };
        }
        catch (Exception) {
          return null;
        }
      });
    }

    public static Bitmap ToBitmap(this BitmapSource bitmapSource) {
      var format = bitmapSource.Format.ToImaging();
      var bmp = new Bitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight, format);

      if (format == System.Drawing.Imaging.PixelFormat.Format8bppIndexed) {
        var cp = bmp.Palette;
        for (var i = 0; i < 256; i++)
          cp.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
        bmp.Palette = cp;
      }

      var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
      bitmapSource.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
      bmp.UnlockBits(data);
      return bmp;
    }

    public static BitmapSource ToBitmapSource(this Bitmap bitmap) {
      var format = bitmap.PixelFormat.ToWPF();

      var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

      var bitmapSource = BitmapSource.Create(bitmapData.Width, bitmapData.Height,
          bitmap.HorizontalResolution, bitmap.VerticalResolution, format, null,
          bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

      bitmap.UnlockBits(bitmapData);
      return bitmapSource;
    }

    public static System.Drawing.Imaging.PixelFormat ToImaging(this System.Windows.Media.PixelFormat pixelFormat) {
      if (pixelFormat == PixelFormats.Indexed8) return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
      if (pixelFormat == PixelFormats.Gray8) return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
      if (pixelFormat == PixelFormats.Bgr24) return System.Drawing.Imaging.PixelFormat.Format24bppRgb;
      if (pixelFormat == PixelFormats.Bgr32) return System.Drawing.Imaging.PixelFormat.Format32bppRgb;
      if (pixelFormat == PixelFormats.Bgra32) return System.Drawing.Imaging.PixelFormat.Format32bppArgb;
      throw new NotImplementedException($"Conversion from pixel format {pixelFormat} is not supported.");
    }

    public static System.Windows.Media.PixelFormat ToWPF(this System.Drawing.Imaging.PixelFormat pixelFormat) {
      return pixelFormat switch {
        System.Drawing.Imaging.PixelFormat.Format1bppIndexed => PixelFormats.BlackWhite,
        System.Drawing.Imaging.PixelFormat.Format4bppIndexed => PixelFormats.Gray4,
        System.Drawing.Imaging.PixelFormat.Format8bppIndexed => PixelFormats.Gray8,
        System.Drawing.Imaging.PixelFormat.Format24bppRgb => PixelFormats.Bgr24,
        System.Drawing.Imaging.PixelFormat.Format32bppRgb => PixelFormats.Bgr32,
        System.Drawing.Imaging.PixelFormat.Format32bppArgb => PixelFormats.Bgra32,
        _ => throw new NotImplementedException($"Conversion from pixel format {pixelFormat} is not supported."),
      };
    }

    public static BitmapSource GetBitmapSource(string filePath) {
      using Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

      var bmp = new BitmapImage();
      bmp.BeginInit();
      bmp.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
      bmp.StreamSource = fileStream;
      bmp.EndInit();
      bmp.Freeze();

      return bmp;
    }

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
