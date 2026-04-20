using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.MediaItem.Video;
using System;
using System.Threading;

namespace PictureManager.Android.Views.Entities;

public class VideoItemV : FrameLayout, ICollectionViewItemContent {
  private readonly ImageView _image;
  private readonly TextView _videoDuration;
  private IDisposable? _videoDurationBinding;
  private CancellationTokenSource? _cts;

  public object? DataContext { get; private set; }
  public View View => this;

  public VideoItemV(Context context) : base(context) {
    _image = new(context);
    _videoDuration = new(context);

    AddView(_image, LPU.FrameMatch());
    AddView(_videoDuration, LPU.Frame(LPU.Wrap, LPU.Wrap));
  }

  public void Bind(object item) {
    DataContext = item;
    if (item is not VideoItemM vi) return;

    if (vi is VideoClipM vc) {
      _videoDuration.Visibility = ViewStates.Visible;
      _videoDurationBinding = vc.Bind(nameof(VideoClipM.Duration), x => x.Duration,
        x => _videoDuration.Text = MH.UI.Controls.MediaPlayer.FormatDuration(x));
    }
    else
      _videoDuration.Visibility = ViewStates.Gone;

    // TODO 
    //_cts = new CancellationTokenSource();
    //_ = ViewModels.MediaItemVM.LoadVideoItemThumbnailAsync(vi, _image, Context!, _cts.Token);
  }

  public void Unbind() {
    _cts?.Cancel();
    _cts?.Dispose();
    _cts = null;
    _videoDurationBinding?.Dispose();
    _videoDurationBinding = null;
    _image.SetImageBitmap(null);
  }
}