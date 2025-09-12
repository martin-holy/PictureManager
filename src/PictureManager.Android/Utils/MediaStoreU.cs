using Android.Content;
using Android.Graphics;
using PictureManager.Common.Features.MediaItem;
using System.Threading.Tasks;

namespace PictureManager.Android.Utils;

public static class MediaStoreU {
  public static async Task<Bitmap?> GetThumbnailBitmapAsync(MediaItemM mi, Context context, int targetSize = 512) {
    return await MH.UI.Android.Utils.MediaStoreU.GetThumbnailBitmapAsync(mi.FilePath, context, targetSize)
      ?? _getThumbnailBitmapFromCustomCache(mi.FilePath, context, targetSize);
  }

  // TODO
  private static Bitmap? _getThumbnailBitmapFromCustomCache(string filePath, Context context, int targetSize) {
    return null;
  }
}