using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MH.UI.WPF.Extensions;

public static class BitmapSourceExtensions {
  public static BitmapSource Create(string filePath, Int32Rect rect = new(), BitmapCacheOption cacheOpt = BitmapCacheOption.OnLoad) {
    using Stream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

    var bmp = new BitmapImage();
    bmp.BeginInit();
    bmp.CacheOption = cacheOpt;
    bmp.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
    bmp.StreamSource = fs;
    if (rect.HasArea) bmp.SourceRect = rect;
    bmp.EndInit();

    return bmp;
  }

  public static CroppedBitmap Crop(this BitmapSource bmp, Int32Rect rect) =>
    new(bmp, rect);

  public static byte[] GetHashPixels(this BitmapSource bmp, int bytes) {
    var scaled = new TransformedBitmap(bmp, new ScaleTransform((double)bytes / bmp.PixelWidth, (double)bytes / bmp.PixelHeight));
    var gray = new FormatConvertedBitmap(scaled, PixelFormats.Gray8, BitmapPalettes.Gray256, 0.0);
    var pixels = new byte[bytes * bytes];
    gray.CopyPixels(pixels, bytes, 0);

    return pixels;
  }

  public static long GetAvgHash(this BitmapSource bmp) =>
    MH.Utils.Imaging.GetBitmapAvgHash(GetHashPixels(bmp, 8));

  public static long GetPerceptualHash(this BitmapSource bmp) =>
    MH.Utils.Imaging.GetBitmapPerceptualHash(GetHashPixels(bmp, 32));

  public static void GetScale(this BitmapSource bmp, int size, out double x, out double y) {
    var pxW = bmp.PixelWidth;
    var pxH = bmp.PixelHeight;
    var sizeW = pxW > pxH ? size : size * (pxW / (pxH / 100.0)) / 100;
    var sizeH = pxH > pxW ? size : size * (pxH / (pxW / 100.0)) / 100;
    x = sizeW / pxW;
    y = sizeH / pxH;
  }

  public static TransformedBitmap Resize(this BitmapSource bmp, double scaleX, double scaleY) =>
    new(bmp, new ScaleTransform(scaleX, scaleY, 0, 0));

  public static TransformedBitmap Resize(this BitmapSource bmp, int size) {
    bmp.GetScale(size, out var scaleX, out var scaleY);
    return bmp.Resize(scaleX, scaleY);
  }

  public static void SaveAsJpeg(this BitmapSource bmp, string filePath, int quality) {
    Directory.CreateDirectory(filePath[..filePath.LastIndexOf(Path.DirectorySeparatorChar)]);
    var encoder = new JpegBitmapEncoder { QualityLevel = quality };
    encoder.Frames.Add(BitmapFrame.Create(bmp));
    using Stream fs = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite);
    encoder.Save(fs);
  }
}