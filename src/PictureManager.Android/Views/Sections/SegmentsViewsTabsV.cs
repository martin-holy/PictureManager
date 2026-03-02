using Android.Content;
using Android.Views;
using MH.UI.Android.Controls.Hosts.TabControlHost;
using MH.UI.Controls;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections;

public sealed class SegmentsViewsTabsV : TabControlHost {
  public SegmentsViewsTabsV(Context context, TabControl dataContext) : base(context, dataContext) { }

  protected override View? _getItemView(Context context, object? item) =>
    item is SegmentsViewVM segmentsView ? new SegmentsViewV(context, segmentsView) : null;
}