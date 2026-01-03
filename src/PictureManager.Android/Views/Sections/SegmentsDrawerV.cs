using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections;

public sealed class SegmentsDrawerV : FrameLayout {
  public SegmentsDrawerV(Context context, SegmentsDrawerVM dataContext) : base(context) {
    AddView(new CollectionViewHost(context, dataContext, SegmentsViewV.GetSegmentV), new LayoutParams(LPU.Match, LPU.Match));
  }
}
