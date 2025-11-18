using Android.Content;
using Android.Widget;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.Segment;
using System;
using System.Threading.Tasks;

namespace PictureManager.Android.Views.Entities;

public sealed class SegmentV : FrameLayout {
  public SegmentV(Context context, SegmentM segment) : base(context) {
    var image = new ImageView(context);
    AddView(image, new LayoutParams(LPU.Match, LPU.Match));
    _loadThumbnailAsync(segment, image);
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
