using MH.UI.WPF.Extensions;
using PictureManager.Domain;
using System;
using System.IO;
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

  public static void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) {
    var dir = Path.GetDirectoryName(destPath);
    if (dir == null) throw new ArgumentException($"Invalid destination path. {destPath}");
    Directory.CreateDirectory(dir);

    using Stream srcFileStream = File.Open(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    var decoder = BitmapDecoder.Create(srcFileStream, BitmapCreateOptions.None, BitmapCacheOption.None);
    if (decoder.Frames[0] == null)
      throw new BadImageFormatException($"Image does not have any frames. {srcPath}");

    var frame = decoder.Frames[0];
    var orientation = (MediaOrientation)((BitmapMetadata)frame.Metadata).GetQuery<ushort>("System.Photo.Orientation", 1);
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
}