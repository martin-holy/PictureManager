using Android.Content;
using Android.Widget;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections;

public sealed class SegmentsViewsV : FrameLayout {
  public SegmentsViewsV(Context context, SegmentsViewsVM dataContext) : base(context) {
    SetBackgroundResource(Resource.Color.c_static_ba);
    AddView(new SegmentsViewsTabsV(context, dataContext.Tabs), new LayoutParams(LPU.Match, LPU.Match));
  }
}