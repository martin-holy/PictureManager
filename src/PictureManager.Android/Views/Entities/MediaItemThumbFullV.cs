using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using System;
using System.Threading.Tasks;

namespace PictureManager.Android.Views.Entities;

public class MediaItemThumbFullV : FrameLayout {
  private readonly ImageView _image;
  private readonly ImageView _videoOverlayer;

  public MediaItemM? DataContext { get; private set; }

  public MediaItemThumbFullV(Context context) : base(context) {
    _image = new(context);
    _videoOverlayer = new(context) { Visibility = ViewStates.Gone };
    _videoOverlayer.SetImageResource(Resource.Drawable.icon_play_circle);

    AddView(_image, new LayoutParams(LPU.Match, LPU.Match));
    AddView(_videoOverlayer, new LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.Center });
  }

  public MediaItemThumbFullV Bind(MediaItemM? mi) {
    DataContext = mi;
    _videoOverlayer.Visibility = mi is VideoM ? ViewStates.Visible : ViewStates.Gone;

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
        return mi switch {
          ImageM => await MediaStoreU.GetImageThumbnail(mi.FilePath, context, 512)
                      ?? ImagingU.CreateImageThumbnail(mi.FilePath, Core.Settings.MediaItem.ThumbSize),
          VideoM => await MediaStoreU.GetVideoThumbnail(mi.FilePath, context, 512)
                      ?? ImagingU.CreateVideoThumbnail(mi.FilePath, Core.Settings.MediaItem.ThumbSize),
          _ => null,
        };
      }
      catch (Exception ex) {
        MH.Utils.Log.Error(ex);
        return null;
      }
    });

    MH.Utils.Tasks.Dispatch(() => imageView.SetImageBitmap(thumbnail?.ApplyOrientation(mi.Orientation)));
  }
}