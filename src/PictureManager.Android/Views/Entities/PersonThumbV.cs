using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.Person;

namespace PictureManager.Android.Views.Entities;

public sealed class PersonThumbV : FrameLayout {
  public PersonThumbV(Context context, PersonM person) : base(context) {
    if (person.Segment is { } segment) {
      AddView(new SegmentV(context, segment), new LayoutParams(LPU.Match, LPU.Match));
    }
    else {
      var noSegmentIcon = new ImageView(context);
      noSegmentIcon.SetImageResource(Resource.Drawable.icon_people);
      AddView(noSegmentIcon, new LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.Center });
    }
  }
}
