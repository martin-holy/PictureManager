using PictureManager.Domain;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace PictureManager.Utils {
  public static class Imaging {
    public static Rotation MediaOrientation2Rotation(MediaOrientation mo) =>
      mo switch {
        MediaOrientation.Rotate90 => Rotation.Rotate270,
        MediaOrientation.Rotate180 => Rotation.Rotate180,
        MediaOrientation.Rotate270 => Rotation.Rotate90,
        _ => Rotation.Rotate0
      };

    public static BitmapImage GetBitmapImage(string filePath, MediaOrientation rotation) {
      var src = new BitmapImage();
      src.BeginInit();
      src.UriSource = new(filePath);
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
      src.Rotation = MediaOrientation2Rotation(rotation);
      src.EndInit();

      return src;
    }

    public static Task CreateThumbnailAsync(MediaType type, string srcPath, string destPath, int size, int rotationAngle, int quality) =>
      type == MediaType.Image
        ? Task.Run(() => CreateImageThumbnail(srcPath, destPath, size, quality))
        : CreateThumbnailAsync(srcPath, destPath, size, rotationAngle, quality);

    public static Task CreateThumbnailAsync(string srcPath, string destPath, int size, int rotationAngle, int quality) {
      var tcs = new TaskCompletionSource<bool>();
      var process = new Process {
        EnableRaisingEvents = true,
        StartInfo = new() {
          Arguments = $"src|\"{srcPath}\" dest|\"{destPath}\" quality|\"{quality}\" size|\"{size}\" rotationAngle|\"{rotationAngle}\"",
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

    public static void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) {
      var dir = Path.GetDirectoryName(destPath);
      if (dir == null) throw new ArgumentException($"Invalid destination path. {destPath}");
      Directory.CreateDirectory(dir);

      using Stream srcFileStream = File.Open(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read);
      var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
      if (decoder.Frames[0] == null)
        throw new BadImageFormatException($"Image does not have any frames. {srcPath}");

      var frame = decoder.Frames[0];
      var orientation = (MediaOrientation)((ushort?)TryGetQuery((BitmapMetadata)frame.Metadata, "System.Photo.Orientation") ?? 1);
      var rotated = orientation is MediaOrientation.Rotate90 or MediaOrientation.Rotate270;
      var pxw = (double)(rotated ? frame.PixelHeight : frame.PixelWidth);
      var pxh = (double)(rotated ? frame.PixelWidth : frame.PixelHeight);

      Domain.Utils.Imaging.GetThumbSize(pxw, pxh, desiredSize, out var thumbWidth, out var thumbHeight);

      var output = new TransformedBitmap(frame, new ScaleTransform(thumbWidth / pxw, thumbHeight / pxh, 0, 0));
      var metadata = new BitmapMetadata("jpg");
      metadata.SetQuery("System.Photo.Orientation", (ushort)orientation);

      using Stream destFileStream = File.Open(destPath, FileMode.Create, FileAccess.ReadWrite);
      var encoder = new JpegBitmapEncoder { QualityLevel = quality };
      encoder.Frames.Add(BitmapFrame.Create(output, null, metadata, frame.ColorContexts));
      encoder.Save(destFileStream);
    }

    public static object TryGetQuery(BitmapMetadata bm, string query) {
      try {
        return bm.GetQuery(query);
      }
      catch (Exception) {
        return null;
      }
    }

    public static void CreateVideoClipThumbnail(string destPath) =>
      CreateThumbnailFromVisual(
        AppCore.FullVideo,
        destPath,
        Core.Settings.ThumbnailSize,
        Core.Settings.JpegQualityLevel);

    public static bool CreateThumbnailFromVisual(FrameworkElement visual, string destPath, int desiredSize, int quality) {
      try {
        // create destination directory if doesn't exist
        var dir = Path.GetDirectoryName(destPath);
        if (dir == null) return false;
        Directory.CreateDirectory(dir);

        // get offset of visual from its parent
        var offset = visual.TranslatePoint(new Point(0, 0), (UIElement)visual.Parent);

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
        var encoder = new JpegBitmapEncoder { QualityLevel = quality };
        encoder.Frames.Add(BitmapFrame.Create(thumb));

        // save thumbnail
        using Stream destFileStream = File.Open(destPath, FileMode.Create, FileAccess.ReadWrite);
        encoder.Save(destFileStream);

        return true;
      }
      catch (Exception) {
        return false;
      }
    }

    public static byte[] GetHashPixels(string filePath, int bytes) {
      // create source
      using Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
      var bmpSource = new BitmapImage();
      bmpSource.BeginInit();
      bmpSource.CacheOption = BitmapCacheOption.None;
      bmpSource.StreamSource = fileStream;
      bmpSource.EndInit();

      // resize
      var scaled = new TransformedBitmap(bmpSource, new ScaleTransform((double)bytes / bmpSource.PixelWidth, (double)bytes / bmpSource.PixelHeight));

      // convert to gray scale
      var grayScale = new FormatConvertedBitmap(scaled, PixelFormats.Gray8, BitmapPalettes.Gray256, 0.0);

      // copy pixels
      var pixels = new byte[bytes * bytes];
      grayScale.CopyPixels(pixels, bytes, 0);

      return pixels;
    }
  }
}
