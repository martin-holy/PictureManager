using System.IO;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Provider;
using PictureManager.AvaloniaUI.Android.Extensions;
using PictureManager.AvaloniaUI.Utils;
using PictureManager.Common.Features.MediaItem;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;
using AndroidBitmap = Android.Graphics.Bitmap;

namespace PictureManager.AvaloniaUI.Android.Utils;

public class Imaging : IImagingP {
  public static long GetImageId(string filePath) {
    var uri = filePath.StartsWith("/storage/emulated")
      ? MediaStore.Images.Media.InternalContentUri
      : MediaStore.Images.Media.ExternalContentUri;

    var cursor = Application.Context.ContentResolver?.Query(
      uri,
      [MediaStore.Images.Media.InterfaceConsts.Id],
      $"{MediaStore.Images.Media.InterfaceConsts.Data}=?",
      [filePath],
      null);

    if (cursor?.MoveToFirst() != true) {
      cursor?.Close();
      return -1;
    }

    var id = cursor.GetLong(0);
    cursor.Close();

    return id;
  }

  public void CreateImageThumbnail(MediaItemM mi) {
    var filePath = mi.FilePath;
    var id = GetImageId(filePath);
    if (id < 0) return;

    var options = new BitmapFactory.Options { InSampleSize = 2 };
    var bitmap = BitmapFactory.DecodeFile(filePath, options);
    if (bitmap == null) return;

    var thumb = AndroidBitmap.CreateScaledBitmap(bitmap, 512, 384, true);
    bitmap.Recycle();

    var values = new ContentValues();
    values.Put(MediaStore.Images.Thumbnails.ImageId, id);
    values.Put(MediaStore.Images.Thumbnails.Kind, (int)ThumbnailKind.MiniKind);

    using var stream = new MemoryStream();
    thumb.Compress(AndroidBitmap.CompressFormat.Jpeg, 80, stream);
    values.Put(MediaStore.Images.Thumbnails.Data, stream.ToArray());

    var uri = filePath.StartsWith("/storage/emulated")
      ? MediaStore.Images.Thumbnails.InternalContentUri
      : MediaStore.Images.Thumbnails.ExternalContentUri;
    Application.Context.ContentResolver!.Insert(uri, values);
    thumb.Recycle();
  }

  public AvaloniaBitmap? GetImageThumbnail(MediaItemM mi) {
    var id = GetImageId(mi.FilePath);
    if (id < 0) return null;

    return MediaStore.Images.Thumbnails.GetThumbnail(
      Application.Context.ContentResolver,
      id,
      ThumbnailKind.MiniKind,
      new() { InSampleSize = 1 })
      .ToAvaloniaBitmap();
  }
}