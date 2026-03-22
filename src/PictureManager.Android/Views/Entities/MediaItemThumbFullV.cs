using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.MediaItem.Video;
using System.Threading;

namespace PictureManager.Android.Views.Entities;

public class MediaItemThumbFullV : FrameLayout, ICollectionViewItemContent {
  private readonly ImageView _image;
  private readonly ImageView _videoOverlayer;
  private CancellationTokenSource? _cts;

  public object? DataContext { get; private set; }
  public View View => this;

  public MediaItemThumbFullV(Context context) : base(context) {
    _image = new(context);
    _videoOverlayer = new(context) { Visibility = ViewStates.Gone };
    _videoOverlayer.SetImageResource(Resource.Drawable.icon_play_circle);

    AddView(_image, LPU.FrameMatch());
    AddView(_videoOverlayer, LPU.Frame(LPU.Wrap, LPU.Wrap, GravityFlags.Center));
  }

  public void Bind(object item) {
    DataContext = item;
    if (item is not MediaItemM mi) return;
    _videoOverlayer.Visibility = mi is VideoM ? ViewStates.Visible : ViewStates.Gone;
    _cts = new CancellationTokenSource();
    _ = ViewModels.MediaItemVM.LoadThumbnailAsync(mi, _image, Context!, _cts.Token);
  }

  public void Unbind() {
    _cts?.Cancel();
    _cts?.Dispose();
    _cts = null;
    _image.SetImageBitmap(null);
  }
}