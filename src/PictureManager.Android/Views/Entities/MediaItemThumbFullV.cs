using Android.Content;
using Android.Widget;
using MH.UI.Android.Utils;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using System;
using System.Threading.Tasks;

namespace PictureManager.Android.Views.Entities;

public class MediaItemThumbFullV : LinearLayout {
  private readonly ImageView _image;

  public MediaItemM? DataContext { get; private set; }

  public MediaItemThumbFullV(Context context) : base(context) {
    _image = new(context);
    AddView(_image, new LayoutParams(LPU.Match, LPU.Match));
  }

  public MediaItemThumbFullV Bind(MediaItemM? mi) {
    DataContext = mi;
    if (mi == null) {
      _image.SetImageBitmap(null);
      return this;
    }

    _loadThumbnailAsync(mi, _image, Context!);
    return this;
  }

  private static async void _loadThumbnailAsync(MediaItemM mi, ImageView imageView, Context context) {
    var thumbnail = await Task.Run(async () => {
      try {
        return await MediaStoreU.GetThumbnailBitmapAsync(mi.FilePath, context, 512)
        ?? ImagingU.CreateImageThumbnail(mi.FilePath, Core.Settings.MediaItem.ThumbSize);
      }
      catch (Exception ex) {
        MH.Utils.Log.Error(ex);
        return null;
      }
    });

    MH.Utils.Tasks.Dispatch(() => imageView.SetImageBitmap(thumbnail));
  }
}