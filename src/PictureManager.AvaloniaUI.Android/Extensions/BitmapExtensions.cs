using System.IO;
using AndroidBitmap = Android.Graphics.Bitmap;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace PictureManager.AvaloniaUI.Android.Extensions;

public static class BitmapExtensions {
  public static AvaloniaBitmap? ToAvaloniaBitmap(this AndroidBitmap? androidBitmap) {
    if (androidBitmap == null) return null;
    using var stream = new MemoryStream();
    androidBitmap.Compress(AndroidBitmap.CompressFormat.Jpeg, 85, stream);
    stream.Position = 0;
    return new(stream);
  }
}