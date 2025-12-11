using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Entities;

public class SegmentRectV : View {
  public SegmentRectV(Context context, SegmentRectM dataContext) : base(context) {
    Background = BackgroundFactory.RoundDarker();

    this.Bind(dataContext, nameof(SegmentRectM.Size), x => x.Size,
      (t, p) => t.LayoutParameters = new FrameLayout.LayoutParams((int)p, (int)p));
    this.Bind(dataContext, nameof(SegmentRectM.X), x => x.X, (t, p) => t.SetX((float)p));
    this.Bind(dataContext, nameof(SegmentRectM.Y), x => x.Y, (t, p) => t.SetY((float)p));
  }
}