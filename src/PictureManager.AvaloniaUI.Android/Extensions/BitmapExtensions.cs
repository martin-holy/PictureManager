using System.IO;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using AndroidBitmap = Android.Graphics.Bitmap;

namespace PictureManager.AvaloniaUI.Android.Extensions;

public static class BitmapExtensions {
  public static AvaloniaBitmap ToAvaloniaBitmap(this AndroidBitmap bmp) {
    var width = bmp.Width;
    var height = bmp.Height;
    var stride = width * (bmp.HasAlpha ? 4 : 3);
    var bmpData = new byte[stride * height];

    bmp.CopyPixelsToBuffer(Java.Nio.ByteBuffer.Wrap(bmpData));

    using var stream = new MemoryStream(bmpData);
    return new(stream);
  }
}