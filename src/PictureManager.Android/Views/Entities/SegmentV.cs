using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.Segment;
using System;
using System.Threading.Tasks;

namespace PictureManager.Android.Views.Entities;

public sealed class SegmentV : FrameLayout, ICollectionViewItemContent {
  private readonly ImageView _image;

  public View View => this;

  public SegmentV(Context context) : base(context) {
    AddView(_image = new ImageView(context), new LayoutParams(LPU.Match, LPU.Match));
  }

  public void Bind(object? item) {
    if (item is not SegmentM segment) return;
    _loadThumbnailAsync(segment, _image);
  }

  public void Unbind() {
    _image.SetImageBitmap(null);
  }

  private static async void _loadThumbnailAsync(SegmentM segment, ImageView imageView) {
    var thumbnail = await Task.Run(() => {
      try {
        return ImagingU.CreateImageRegionThumbnail(
          segment.MediaItem.FilePath, (int)segment.X, (int)segment.Y, (int)segment.Size, SegmentVM.SegmentSize);
      }
      catch (Exception ex) {
        MH.Utils.Log.Error(ex);
        return null;
      }
    });

    MH.Utils.Tasks.Dispatch(() => imageView.SetImageBitmap(thumbnail?.ApplyOrientation(segment.MediaItem.Orientation)));
  }
}