using MH.Utils;
using SkiaSharp;
using System;
using System.IO;

namespace PictureManager.AvaloniaUI.Utils;

public static class Imaging {
  public static void CreateImageThumbnail(string srcPath, string destPath, int desiredSize, int quality) {
    var dir = Path.GetDirectoryName(destPath);
    if (dir == null) throw new ArgumentException($"Invalid destination path. {destPath}");
    Directory.CreateDirectory(dir);

    using Stream srcFileStream = File.Open(srcPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    using var original = SKBitmap.Decode(srcFileStream);

    // TODO PORT check for correct image format

    // TODO PORT read orientation
    var orientation = Orientation.Normal;
    var rotated = orientation is Orientation.Rotate90 or Orientation.Rotate270;
    var pxw = (double)(rotated ? original.Height : original.Width);
    var pxh = (double)(rotated ? original.Width : original.Height);

    MH.Utils.Imaging.GetThumbSize(pxw, pxh, desiredSize, out var thumbWidth, out var thumbHeight);

    using var resized = original.Resize(new SKImageInfo(thumbWidth, thumbHeight), SKFilterQuality.Medium);
    // TODO PORT write orientation
    using var image = SKImage.FromBitmap(resized);
    using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
    using var outputStream = File.OpenWrite(destPath);
    data.SaveTo(outputStream);
  }
}