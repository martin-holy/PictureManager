using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Controls;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections;

public sealed class SegmentsViewsTabsV(Context context, TabControl dataContext) : TabControlHost(context, dataContext) {
  protected override View? _getItemView(LinearLayout container, object? item) =>
    item is SegmentsViewVM segmentsView ? new SegmentsViewV(container.Context!, segmentsView) : null;
}