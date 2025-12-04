using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Android.Views.Entities;
using PictureManager.Common;
using PictureManager.Common.Features.Keyword;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Sections;

public sealed class PersonDetailV : LinearLayout {
  private readonly PersonDetailVM _dataContext;
  private readonly TreeMenu _menu;
  private readonly TextView _personName;
  private readonly IconItemsLayout _keywords;

  public PersonDetailV(Context context, PersonDetailVM dataContext) : base(context) {
    Orientation = Orientation.Vertical;

    _dataContext = dataContext;
    _menu = new(context, _ => dataContext.Menu);

    var iconMenu = new IconButton(context)
      .WithClickAction(this, (o, s) => o._menu.ShowItemMenu(s, o._dataContext.PersonM));
    iconMenu.SetImageDrawable(Icons.GetIcon(context, Res.IconThreeBars));

    _personName = new TextView(context) {
      TextSize = DisplayU.DpToPx(12),
      TextAlignment = TextAlignment.Center
    };
    _personName.SetPadding(DimensU.Spacing);

    var iconMenuAndName = new FrameLayout(context) { Background = BackgroundFactory.Dark() };
    iconMenuAndName.AddView(_personName, new FrameLayout.LayoutParams(LPU.Match, LPU.Wrap));
    iconMenuAndName.AddView(iconMenu, new FrameLayout.LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.CenterVertical | GravityFlags.Left }.WithDpMargin(2));

    _keywords = new IconItemsLayout(context, Res.IconTag, _getKeywordView) {
      MaxHeight = DisplayU.DpToPx((float)(DimensU.IconSize * 1.5))
    };

    AddView(iconMenuAndName, new LayoutParams(LPU.Match, LPU.Wrap).WithDpMargin(2, 0, 2, 0));
    AddView(_keywords, new LayoutParams(LPU.Match, LPU.Wrap).WithDpMargin(2, 2, 2, 0));
    AddView(new CollectionViewHost(context, dataContext.TopSegments, _getSegmentView), new LayoutParams(LPU.Match, _getTopSegmentsHeight()));
    AddView(new CollectionViewHost(context, dataContext.AllSegments, _getSegmentView), new LayoutParams(LPU.Match, LPU.Wrap));

    this.Bind<PersonDetailV, PersonDetailVM, string>(
      dataContext,
      [nameof(PersonDetailVM.PersonM), nameof(PersonM.Name)],
      [s => (s as PersonDetailVM)?.PersonM, s => (s as PersonM)?.Name],
      (t, v) => t._personName.Text = v);

    this.Bind<PersonDetailV, PersonDetailVM, KeywordM[]>(
      dataContext,
      [nameof(PersonDetailVM.PersonM), nameof(PersonM.DisplayKeywords)],
      [s => (s as PersonDetailVM)?.PersonM, s => (s as PersonM)?.DisplayKeywords],
      (t, v) => {
        _keywords.Visibility = v.Length == 0 ? ViewStates.Gone : ViewStates.Visible;
        _keywords.WrapLayout.Items = v.Length == 0 ? null : v;
      });
  }

  private SegmentV? _getSegmentView(LinearLayout container, ICollectionViewGroup group, object? item) =>
    item is SegmentM segment ? new SegmentV(container.Context!, segment) : null;

  private static int _getTopSegmentsHeight() =>
    SegmentVM.SegmentUiFullWidth + (CollectionView.ItemBorderSize * 2) + // segment with border
    DimensU.IconButtonSize + (DimensU.Spacing * 2) + // group height
    DimensU.Spacing; // spacing

  private TextView? _getKeywordView(object item) =>
    item is IListItem li
      ? new TextView(Context) { Text = li.Name, Background = BackgroundFactory.RoundDarker() }
      : null;
}
