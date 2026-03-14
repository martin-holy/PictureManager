using Android.Content;
using Android.Widget;
using MH.UI.Android.Binding;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils.Disposables;
using PictureManager.Android.Views.Sections;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Dialogs;

public sealed class MergePeopleDialogV : LinearLayout {
  public MergePeopleDialogV(Context context, MergePeopleDialog dataContext, BindingScope bindings) : base(context) {
    Orientation = Orientation.Vertical;
    LayoutParameters = LPU.Linear(LPU.Match, 0, 1f);

    var people = new CollectionViewHost(context, dataContext.PeopleView, PeopleV.CreatePersonV);
    var personName = new TextView(context) { Background = BackgroundFactory.RoundDarker() }
      .BindText(dataContext, nameof(MergePeopleDialog.Person), x => x.Person.Name, x => x, bindings);
    var segments = new CollectionViewHost(context, dataContext.SegmentsView, SegmentsViewV.CreateSegmentV);

    AddView(people, LPU.Linear(LPU.Match, _getPeopleViewHeight()));
    AddView(personName, LPU.LinearWrap().WithMargin(DimensU.Spacing));
    AddView(segments, LPU.Linear(LPU.Match, 0, 1f));
  }

  private static int _getPeopleViewHeight() =>
    SegmentVM.SegmentUiFullWidth + (CollectionView.ItemBorderSize * 2) + // segment with border
    DimensU.IconButtonSize + (DimensU.Spacing * 2) + // group height
    DimensU.Spacing; // spacing
}