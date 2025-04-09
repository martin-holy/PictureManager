using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Provider;
using PictureManager.AvaloniaUI.Android.Extensions;
using PictureManager.AvaloniaUI.Utils;
using PictureManager.Common.Features.MediaItem;
using System.IO;
using AndroidBitmap = Android.Graphics.Bitmap;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace PictureManager.AvaloniaUI.Android.Utils;

public class Imaging : IImagingP {
  public static long GetImageId(string filePath) {
    if (MediaStore.Images.Media.ExternalContentUri is not { } uri) return -1;

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

    if (MediaStore.Images.Thumbnails.ExternalContentUri is not { } uri) return;

    var id = GetImageId(filePath);
    if (id < 0) {
      id = InsertNewImage(filePath);
      if (id < 0) return;
    }

    var options = new BitmapFactory.Options { InSampleSize = 2 };
    var bitmap = BitmapFactory.DecodeFile(filePath, options);
    if (bitmap == null) return;

    using var stream = new MemoryStream();
    var thumb = AndroidBitmap.CreateScaledBitmap(bitmap, 512, 384, true);
    thumb.Compress(AndroidBitmap.CompressFormat.Jpeg, 85, stream);

    var values = new ContentValues(3);
    values.Put(MediaStore.Images.Thumbnails.ImageId, id);
    values.Put(MediaStore.Images.Thumbnails.Kind, (int)ThumbnailKind.MiniKind);
    values.Put(MediaStore.Images.Thumbnails.Data, stream.ToArray());

    Application.Context.ContentResolver!.Insert(uri, values);
    thumb.Recycle();
    bitmap.Recycle();
  }

  public static long InsertNewImage(string filePath) {
    if (MediaStore.Images.Media.ExternalContentUri is not { } uri) return -1;

    var values = new ContentValues(2);
    values.Put(MediaStore.Images.Media.InterfaceConsts.Data, filePath);
    values.Put(MediaStore.Images.Media.InterfaceConsts.MimeType, "image/jpeg");
    
    var imgUri = Application.Context.ContentResolver?.Insert(uri, values);
    
    return long.TryParse(imgUri?.LastPathSegment, out var id)
      ? id
      : GetImageId(filePath);
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