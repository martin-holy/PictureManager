using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Interfaces;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections;

public sealed class SegmentsViewV : LinearLayout {
  public SegmentsViewV(Context context, SegmentsViewVM dataContext) : base(context) {
    Orientation = Orientation.Vertical;
    SetBackgroundResource(Resource.Color.c_static_ba);
    AddView(new CollectionViewHost(context, dataContext, CreateSegmentV), new LayoutParams(LPU.Match, 0, 1f).WithMargin(0, 0, 0, DimensU.Spacing));
    AddView(new CollectionViewHost(context, dataContext.CvPeople, PeopleV.CreatePersonV), new LayoutParams(LPU.Match, 0, 1f).WithMargin(0, DimensU.Spacing, 0, 0));
  }

  public static ICollectionViewItemContent CreateSegmentV(Context context, ICollectionViewGroup group) =>
    new SegmentV(context);
}