using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.Segment;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Android.Views.Entities;

public sealed class SegmentV : FrameLayout, ICollectionViewItemContent {
  private readonly ImageView _image;
  private CancellationTokenSource? _cts;

  public View View => this;

  public SegmentV(Context context) : base(context) {
    AddView(_image = new ImageView(context), new LayoutParams(LPU.Match, LPU.Match));
  }

  public void Bind(object item) {
    Unbind();
    if (item is not SegmentM segment) return;
    _cts = new CancellationTokenSource();
    _ = _loadThumbnailAsync(segment, _image, _cts.Token);
  }

  public void Unbind() {
    _cts?.Cancel();
    _cts?.Dispose();
    _cts = null;
    _image.SetImageBitmap(null);
  }

  private static async Task _loadThumbnailAsync(SegmentM segment, ImageView imageView, CancellationToken token) {
    try {
      var bitmap = await Task.Run(() => {
        try {
          token.ThrowIfCancellationRequested();

          var thumb = ImagingU.CreateImageRegionThumbnail(
            segment.MediaItem.FilePath, (int)segment.X, (int)segment.Y, (int)segment.Size, SegmentVM.SegmentSize);

          token.ThrowIfCancellationRequested();

          return thumb?.ApplyOrientation(segment.MediaItem.Orientation);
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