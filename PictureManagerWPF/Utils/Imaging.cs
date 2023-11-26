using PictureManager.Domain;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PictureManager.Utils;

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

  public static void CreateThumbnail(string srcPath, string destPath, int size, int rotationAngle, int quality) =>
    CreateThumbnailAsync(srcPath, destPath, size, rotationAngle, quality).GetAwaiter().GetResult();

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

    process.Exited += (_, _) => {
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

    MH.Utils.Imaging.GetThumbSize(pxw, pxh, desiredSize, out var thumbWidth, out var thumbHeight);

    var output = new TransformedBitmap(frame, new ScaleTransform(thumbWidth / pxw, thumbHeight / pxh, 0, 0));
    var metadata = new BitmapMetadata("jpg");
    metadata.SetQuery("System.Photo.Orientation", (ushort)orientation);

    using Stream destFileStream = File.Open(destPath, FileMode.Create, FileAccess.ReadWrite);
    var encoder = new JpegBitmapEncoder { QualityLevel = quality };
    encoder.Frames.Add(BitmapFrame.Create(output, null, metadata, null));
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
}