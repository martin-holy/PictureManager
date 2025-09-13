using Android.Content;
using Android.Graphics;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Video;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PictureManager.Android.Utils;

public static class MediaStoreU {
  public static async Task<Bitmap?> GetThumbnailBitmapAsync(MediaItemM mi, Context context, int targetSize = 512) {
    return await MH.UI.Android.Utils.MediaStoreU.GetThumbnailBitmapAsync(mi.FilePath, context, targetSize)
      ?? _getThumbnailBitmapFromCustomCache(mi);
  }

  private static Bitmap? _getThumbnailBitmapFromCustomCache(MediaItemM mi) {
    try {
      if (!File.Exists(mi.FilePathCache)) {
        if (!File.Exists(mi.FilePath)) {
          Core.R.MediaItem.ItemDelete(mi is VideoItemM vmi ? vmi.Video : mi);
          return null;
        }

        MH.UI.Android.Utils.ImagingU.CreateImageThumbnail(
          mi.FilePath,
          mi.FilePathCache,
          Core.Settings.MediaItem.ThumbSize,
          Core.Settings.Common.JpegQuality);
      }

      return BitmapFactory.DecodeFile(mi.FilePathCache);
    }
    catch (Exception ex) {
      MH.Utils.Log.Error(ex);
      return null;
    }
  }
}