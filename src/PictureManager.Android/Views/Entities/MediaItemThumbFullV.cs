using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Utils;
using PictureManager.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Android.Views.Entities;

public class MediaItemThumbFullV : FrameLayout, ICollectionViewItemContent {
  private readonly ImageView _image;
  private readonly ImageView _videoOverlayer;
  private CancellationTokenSource? _cts;

  public View View => this;

  public MediaItemThumbFullV(Context context) : base(context) {
    _image = new(context);
    _videoOverlayer = new(context) { Visibility = ViewStates.Gone };
    _videoOverlayer.SetImageResource(Resource.Drawable.icon_play_circle);

    AddView(_image, new LayoutParams(LPU.Match, LPU.Match));
    AddView(_videoOverlayer, new LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.Center });
  }

  public void Bind(object item) {
    Unbind();
    if (item is not MediaItemM mi) return;
    _videoOverlayer.Visibility = mi is VideoM ? ViewStates.Visible : ViewStates.Gone;
    _cts = new CancellationTokenSource();
    _ = _loadThumbnailAsync(mi, _image, Context!, _cts.Token);
  }

  public void Unbind() {
    _cts?.Cancel();
    _cts?.Dispose();
    _cts = null;
    _image.SetImageBitmap(null);
  }

  private static async Task _loadThumbnailAsync(MediaItemM mi, ImageView imageView, Context context, CancellationToken token) {
    try {
      var bitmap = await Task.Run(async () => {
        try {
          token.ThrowIfCancellationRequested();

          var filePath = mi.FilePath;
          var thumbSize = Core.Settings.MediaItem.ThumbSize;

          var thumb = mi switch {
            ImageM => await MediaStoreU.GetImageThumbnail(filePath, context, 512)
                      ?? ImagingU.CreateImageThumbnail(filePath, thumbSize),
            VideoM => await MediaStoreU.GetVideoThumbnail(filePath, context, 512)
                      ?? ImagingU.CreateVideoThumbnail(filePath, thumbSize),
            _ => null
          };

          token.ThrowIfCancellationRequested();

          return thumb?.ApplyOrientation(mi.Orientation);
        }
        catch (OperationCanceledException) {
          throw;
        }
        catch (Exception ex) {
          MH.Utils.Log.Error(ex);
          return null;
        }
      }, token);

      if (token.IsCancellationRequested) return;

      imageView.Post(() => {
        if (!token.IsCancellationRequested)
          imageView.SetImageBitmap(bitmap);
      });
    }
    catch (OperationCanceledException) {
      // ignored
    }
  }
}