using SkiaSharp;
using System.Runtime.InteropServices;
using AndroidBitmap = Android.Graphics.Bitmap;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace PictureManager.AvaloniaUI.Android.Extensions;

public static class BitmapExtensions {
  public static AvaloniaBitmap ToAvaloniaBitmap(this AndroidBitmap androidBitmap) {
    var info = new SKImageInfo(androidBitmap.Width, androidBitmap.Height, SKColorType.Rgba8888);
    var skBitmap = new SKBitmap(info);

    // Copy pixels
    var pixels = new byte[androidBitmap.ByteCount];
    androidBitmap.CopyPixelsToBuffer(Java.Nio.ByteBuffer.Wrap(pixels));

    // Pin array and set pixels
    var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
    try {
      skBitmap.InstallPixels(info, handle.AddrOfPinnedObject());
    }
    finally {
      handle.Free();
    }

    return new(SKImage.FromBitmap(skBitmap).Encode().AsStream());
  }
}