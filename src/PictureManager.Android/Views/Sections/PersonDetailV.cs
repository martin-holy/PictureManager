using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Common;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;
using CVH = MH.UI.Android.Controls.Hosts.CollectionViewHost;

namespace PictureManager.Android.Views.Sections;

public sealed class PersonDetailV : LinearLayout {
  private readonly TextView _personName;
  private readonly IconItemsLayout _keywords;

  public PersonDetailV(Context context, PersonDetailVM dataContext) : base(context) {
    Orientation = Orientation.Vertical;

    _personName = new TextView(context) {
      TextSize = 24f,
      TextAlignment = TextAlignment.Center,
      Background = BackgroundFactory.Dark()
    };
    _personName.SetPadding(DimensU.Spacing);

    _keywords = new IconItemsLayout(context, Res.IconTag, _getKeywordView) {
      MaxHeight = DisplayU.DpToPx((float)(DimensU.IconSize * 1.5))
    };

    AddView(_personName, new LayoutParams(LPU.Match, LPU.Wrap) { Gravity = GravityFlags.Center }.WithDpMargin(2, 0, 2, 0));
    AddView(_keywords, new LayoutParams(LPU.Match, LPU.Wrap).WithDpMargin(2, 2, 2, 0));
    AddView(new CVH.CollectionViewHost(context, dataContext.TopSegments, SegmentsViewV.CreateSegmentV), new LayoutParams(LPU.Match, _getTopSegmentsHeight()));
    AddView(new CVH.CollectionViewHost(context, dataContext.AllSegments, SegmentsViewV.CreateSegmentV), new LayoutParams(LPU.Match, LPU.Wrap));

    this.Bind<PersonDetailV, string>(
      dataContext,
      [nameof(PersonDetailVM.PersonM), nameof(PersonM.Name)],
      [s => (s as PersonDetailVM)?.PersonM, s => (s as PersonM)?.Name],
      (t, v) => t._personName.Text = v);

    this.Bind<PersonDetailV, KeywordM[]>(
      dataContext,
      [nameof(PersonDetailVM.PersonM), nameof(PersonM.DisplayKeywords)],
      [s => (s as PersonDetailVM)?.PersonM, s => (s as PersonM)?.DisplayKeywords],
      (t, v) => {
        var empty = v == null || v.Length == 0;
        _keywords.Visibility = empty ? ViewStates.Gone : ViewStates.Visible;
        _keywords.WrapLayout.Items = empty ? null : v;
      });
  }

  private static int _getTopSegmentsHeight() =>
    SegmentVM.SegmentUiFullWidth + (CollectionView.ItemBorderSize * 2) + // segment with border
    DimensU.IconButtonSize + (DimensU.Spacing * 2) + // group height
    DimensU.Spacing; // spacing

  private TextView? _getKeywordView(object item) =>
    item is IListItem li
      ? new TextView(Context) { Text = li.Name, Background = BackgroundFactory.RoundDarker() }
      : null;
}
