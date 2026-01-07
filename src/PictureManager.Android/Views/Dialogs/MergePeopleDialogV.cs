using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using PictureManager.Android.Views.Sections;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Dialogs;

public sealed class MergePeopleDialogV : LinearLayout {
  public MergePeopleDialogV(Context context, MergePeopleDialog dataContext) : base(context) {
    Orientation = Orientation.Vertical;
    LayoutParameters = new LinearLayout.LayoutParams(LPU.Match, 0, 1f);

    AddView(new CollectionViewHost(context, dataContext.PeopleView, PeopleV.GetPersonV),
      new LayoutParams(LPU.Match, _getPeopleViewHeight()));

    AddView(new TextView(context) { Background = BackgroundFactory.RoundDarker() }
      .BindText(dataContext, nameof(MergePeopleDialog.Person), x => x.Person.Name, x => x, out var _),
      new LayoutParams(LPU.Wrap, LPU.Wrap).WithMargin(DimensU.Spacing));

    AddView(new CollectionViewHost(context, dataContext.SegmentsView, SegmentsViewV.GetSegmentV),
      new LayoutParams(LPU.Match, 0, 1f));
  }

  private static int _getPeopleViewHeight() =>
    SegmentVM.SegmentUiFullWidth + (CollectionView.ItemBorderSize * 2) + // segment with border
    DimensU.IconButtonSize + (DimensU.Spacing * 2) + // group height
    DimensU.Spacing; // spacing
}
