using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;
using System;

namespace PictureManager.Android.Views.Entities;

public sealed class PersonThumbV : FrameLayout, ICollectionViewItemContent {
  private readonly IconView _noSegmentIcon;
  private readonly SegmentV _segmentV;
  private IDisposable? _personSegmentBinding;

  public View View => this;

  public PersonThumbV(Context context) : base(context) {
    _noSegmentIcon = new IconView(context).Bind(Resource.Drawable.icon_people, Resource.Color.gray5);
    _segmentV = new(context);

    AddView(_noSegmentIcon, 0, new LayoutParams(LPU.Match, LPU.Match) { Gravity = GravityFlags.Center }.WithDpMargin(DimensU.Spacing));
    AddView(_segmentV, 0, new LayoutParams(LPU.Match, LPU.Match));
  }

  public void Bind(object item) {
    Unbind();
    if (item is not PersonM person) return;
    _personSegmentBinding = this.Bind(person, nameof(PersonM.Segment), x => x.Segment, _onSegmentChanged);
  }

  public void Unbind() {
    _personSegmentBinding?.Dispose();
    _personSegmentBinding = null;
    _segmentV.Unbind();
  }

  private void _onSegmentChanged(PersonThumbV self, SegmentM? segment) {
    _segmentV.Unbind();

    if (segment == null) {
      _noSegmentIcon.Visibility = ViewStates.Visible;
      _segmentV.Visibility = ViewStates.Gone;
    }
    else {
      _segmentV.Bind(segment);
      _noSegmentIcon.Visibility = ViewStates.Gone;
      _segmentV.Visibility = ViewStates.Visible;
    }
  }
}