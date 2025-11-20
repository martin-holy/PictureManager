using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Interfaces;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections;

public sealed class PersonDetailV : LinearLayout {
  public PersonDetailV(Context context, PersonDetailVM dataContext) : base(context) {
    Orientation = Orientation.Vertical;

    var name = new TextView(context) {
      Text = dataContext.PersonM?.Name,
      TextSize = DisplayU.DpToPx(12),
      Background = BackgroundFactory.Dark(),
      TextAlignment = TextAlignment.Center
    };
    name.SetPadding(DimensU.Spacing);

    AddView(name, new LayoutParams(LPU.Match, LPU.Wrap).WithDpMargin(2, 0, 2, 0));

    AddView(new CollectionViewHost(context, dataContext.AllSegments, _getSegmentView));
  }

  private SegmentV? _getSegmentView(LinearLayout container, ICollectionViewGroup group, object? item) =>
    item is SegmentM segment ? new SegmentV(container.Context!, segment) : null;
}
