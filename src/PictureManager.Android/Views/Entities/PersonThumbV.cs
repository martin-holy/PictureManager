using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.Person;
using PictureManager.Common.Features.Segment;

namespace PictureManager.Android.Views.Entities;

public sealed class PersonThumbV : FrameLayout {
  public PersonThumbV(Context context, PersonM person) : base(context) {
    this.Bind(person, nameof(PersonM.Segment), x => x.Segment, _onSegmentChanged);
  }

  private static void _onSegmentChanged(PersonThumbV self, SegmentM? segment) {
    if (self.ChildCount > 0 && self.GetChildAt(0) is { } child) {
      self.RemoveViewAt(0);
      child.Dispose();
    }

    if (segment == null) {
      self.AddView(new IconView(self.Context!)
        .Bind(Resource.Drawable.icon_people, Resource.Color.gray5),
        0, new LayoutParams(LPU.Match, LPU.Match) { Gravity = GravityFlags.Center }.WithDpMargin(DimensU.Spacing));
    }
    else
      self.AddView(new SegmentV(self.Context!, segment), 0, new LayoutParams(LPU.Match, LPU.Match));
  }
}
