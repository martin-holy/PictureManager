using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.UI.Controls;
using MH.UI.Interfaces;
using MH.Utils;
using PictureManager.Android.Views.Entities;
using PictureManager.Common;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;
using System;

namespace PictureManager.Android.Views.Sections;

public sealed class PersonDetailV : LinearLayout {
  private readonly PersonDetailVM _dataContext;
  private readonly TreeMenu _menu;
  private readonly TextView _personName;
  private IDisposable? _personBind;

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

    AddView(iconMenuAndName, new LayoutParams(LPU.Match, LPU.Wrap).WithDpMargin(2, 0, 2, 0));
    AddView(new CollectionViewHost(context, dataContext.TopSegments, _getSegmentView), new LayoutParams(LPU.Match, _getTopSegmentsHeight()));
    AddView(new CollectionViewHost(context, dataContext.AllSegments, _getSegmentView), new LayoutParams(LPU.Match, LPU.Wrap));

    // TODO Binding: review this. try to create nested binding. what if x.PersonM is null?
    // maybe: if person is null, than bind to x.PersonM to know when to drop the bind to x.PersonM and bind to PersonM.Name
    this.Bind(dataContext, x => x.PersonM, (pd, person) => {
      pd._personName.Text = person?.Name;
      _personBind?.Dispose();

      if (person != null)
        _personBind = pd.Bind(person, x => x.Name, (pd, name) => pd._personName.Text = name);
    });
  }

  private SegmentV? _getSegmentView(LinearLayout container, ICollectionViewGroup group, object? item) =>
    item is SegmentM segment ? new SegmentV(container.Context!, segment) : null;

  private static int _getTopSegmentsHeight() =>
    SegmentVM.SegmentUiFullWidth + (CollectionView.ItemBorderSize * 2) + // segment with border
    DimensU.IconButtonSize + (DimensU.Spacing * 2) + // group height
    DimensU.Spacing; // spacing
}
