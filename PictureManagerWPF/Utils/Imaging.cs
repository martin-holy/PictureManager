using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Properties;

namespace PictureManager.Utils {
  public static class Imaging {
    public static void ResizeJpg(string src, string dest, int px, bool withMetadata, bool withThumbnail) {
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

      using (Stream srcFileStream = File.Open(srcFile.FullName, FileMode.Open, FileAccess.Read)) {
        var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
        if (decoder.CodecInfo == null || !decoder.CodecInfo.FileExtensions.Contains("jpg") || decoder.Frames[0] == null) return;

        var firstFrame = decoder.Frames[0];

        var pxw = firstFrame.PixelWidth; // image width
        var pxh = firstFrame.PixelHeight; // image height
        var gcd = GreatestCommonDivisor(pxw, pxh);
        var rw = pxw / gcd; // image ratio
        var rh = pxh / gcd; // image ratio
        var q = Math.Sqrt((double)px / (rw * rh)); // Bulgarian constant
        var stw = (q * rw) / pxw; // scale transform X
        var sth = (q * rh) / pxh; // scale transform Y

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

        var encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };

        encoder.Frames.Add(BitmapFrame.Create(resized, thumbnail, metadata, firstFrame.ColorContexts));

        using (Stream destFileStream = File.Open(destFile.FullName, FileMode.Create, FileAccess.ReadWrite)) {
          encoder.Save(destFileStream);
        }

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
    }

    public static void ReSaveImage(string filePath) {
      // TODO: try to preserve EXIF information
      var original = new FileInfo(filePath);
      var newFile = new FileInfo(filePath + "_newFile");
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
        App.Ui.LogError(ex, filePath);
      }
    }

    public static Task CreateThumbnailsAsync(IReadOnlyCollection<MediaItem> items, CancellationToken token) {
      return Task.Run(async () => {
        var count = items.Count;
        var workingOn = 0;

        await Task.WhenAll(
          from partition in Partitioner.Create(items).GetPartitions(Environment.ProcessorCount)
          select Task.Run(async delegate {
            using (partition) {
              while (partition.MoveNext()) {
                if (token.IsCancellationRequested) break;

                workingOn++;
                var workingOnInt = workingOn;
                Application.Current.Dispatcher?.Invoke(delegate {
                  App.Ui.AppInfo.ProgressBarValueB = Convert.ToInt32((double)workingOnInt / count * 100);
                });

                var mi = partition.Current;
                // Folder can by null if the mediaItem is corrupted and is deleted in loading metadata process
                if (mi == null || mi.Folder == null) continue;
                if (File.Exists(mi.FilePathCache)) continue;
                await CreateThumbnailAsync(mi.MediaType, mi.FilePath, mi.FilePathCache, Settings.Default.ThumbnailSize, 0);

                mi.ReloadThumbnail();
              }
            }
          }));
      });
    }

    public static Task CreateThumbnailAsync(MediaType type, string srcPath, string destPath, int size, int rotationAngle) {
      return type == MediaType.Image
        ? Task.Run(() => CreateImageThumbnail(srcPath, destPath, size))
        : CreateThumbnailAsync(srcPath, destPath, size, rotationAngle);
    }

    public static bool CreateImageThumbnail(string srcPath, string destPath, int desiredSize) {
      try {
        var dir = Path.GetDirectoryName(destPath);
        if (dir == null) return false;
        Directory.CreateDirectory(dir);

        using (Stream srcFileStream = File.Open(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
          var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
          if (decoder.CodecInfo == null || !decoder.CodecInfo.FileExtensions.Contains("jpg") ||
              decoder.Frames[0] == null) return false;

          var frame = decoder.Frames[0];
          var orientation = (MediaOrientation)((ushort?)((BitmapMetadata)frame.Metadata)?.GetQuery("System.Photo.Orientation") ?? 1);
          var rotated = orientation == MediaOrientation.Rotate90 ||
                        orientation == MediaOrientation.Rotate270;
          var pxw = (double)(rotated ? frame.PixelHeight : frame.PixelWidth);
          var pxh = (double)(rotated ? frame.PixelWidth : frame.PixelHeight);
          
          Domain.Utils.Imaging.GetThumbSize(pxw, pxh, desiredSize, out var thumbWidth, out var thumbHeight);

          var output = new TransformedBitmap(frame, new ScaleTransform(thumbWidth / pxw, thumbHeight / pxh, 0, 0));

          if (rotated) {
            // yes, angles 90 and 270 are switched
            var angle = orientation == MediaOrientation.Rotate90 ? 270 : 90;
            output = new TransformedBitmap(output, new RotateTransform(angle));
          }

          var encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };
          encoder.Frames.Add(BitmapFrame.Create(output));

          using (Stream destFileStream = File.Open(destPath, FileMode.Create, FileAccess.ReadWrite)) {
            encoder.Save(destFileStream);
          }
        }

        return true;
      }
      catch (Exception) {
        return false;
      }
    }

    public static Task CreateThumbnailAsync(string srcPath, string destPath, int size, int rotationAngle) {
      var tcs = new TaskCompletionSource<bool>();
      var process = new Process {
        EnableRaisingEvents = true,
        StartInfo = new ProcessStartInfo {
          Arguments = $"src|\"{srcPath}\" dest|\"{destPath}\" quality|\"{80}\" size|\"{size}\" rotationAngle|\"{rotationAngle}\"",
          FileName = "ThumbnailCreator.exe",
          UseShellExecute = false,
          CreateNoWindow = true
        }
      };

      process.Exited += (s, e) => {
        tcs.TrySetResult(true);
        process.Dispose();
      };

      process.Start();
      return tcs.Task;
    }

    public static Task<int[]> GetImageDimensionsAsync(string filePath) {
      return Task.Run(() => {
        try {
          using (Stream srcFileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
            var frame = decoder.Frames[0];
            return new[] { frame.PixelWidth, frame.PixelHeight };
          }
        }
        catch (Exception) {
          return null;
        }
      });
    }

    // Create Thumbnail from video from given time (very slow!!!)
    public static Task CreateVideoThumbnailAsync(string srcPath, string destPath, string size, string time) {
      var args = $"-y -i \"{srcPath}\" -ss {time} -frames:v 1 -s {size} \"{destPath}\"";
      var tcs = new TaskCompletionSource<bool>();

      var process = new Process {
        EnableRaisingEvents = true,
        StartInfo = new ProcessStartInfo {
          Arguments = args,
          FileName = Settings.Default.FfmpegPath,
          UseShellExecute = false,
          CreateNoWindow = true
        }
      };

      process.Exited += (s, e) => {
        tcs.TrySetResult(true);
        process.Dispose();
      };

      process.Start();
      return tcs.Task;
    }

    public static bool CreateVideoThumbnailFromVisual(FrameworkElement visual, string destPath, int desiredSize) {
      try {
        // create destination directory if doesn't exist
        var dir = Path.GetDirectoryName(destPath);
        if (dir == null) return false;
        Directory.CreateDirectory(dir);

        // get offset of visual from its parent
        var offset = visual.TranslatePoint(new Point(0, 0), (UIElement) visual.Parent);

        // round all variables for safer calculation
        var ox = Math.Round(offset.X, 0);
        var oy = Math.Round(offset.Y, 0);
        var aw = Math.Round(visual.ActualWidth, 0);
        var ah = Math.Round(visual.ActualHeight, 0);

        // render visual to bitmap
        var bmp = new RenderTargetBitmap((int)(aw + ox), (int)(ah + oy), 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);

        // crop bitmap
        var crop = new CroppedBitmap(bmp, new Int32Rect((int)ox, (int)oy, (int)aw, (int)ah));

        // scale bitmap to thumbnail
        Domain.Utils.Imaging.GetThumbSize(aw, ah, desiredSize, out var tw, out var th);
        var thumb = new TransformedBitmap(crop, new ScaleTransform(tw / aw, th / ah, 0, 0));

        // create encoder for thumbnail
        var encoder = new JpegBitmapEncoder { QualityLevel = Settings.Default.JpegQualityLevel };
        encoder.Frames.Add(BitmapFrame.Create(thumb));

        // save thumbnail
        using (Stream destFileStream = File.Open(destPath, FileMode.Create, FileAccess.ReadWrite)) {
          encoder.Save(destFileStream);
        }

        return true;
      }
      catch (Exception) {
        return false;
      }
    }
  }
}
