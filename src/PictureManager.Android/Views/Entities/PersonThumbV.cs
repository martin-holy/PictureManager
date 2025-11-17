using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Utils;
using PictureManager.Common.Features.Person;

namespace PictureManager.Android.Views.Entities;

public sealed class PersonThumbV : FrameLayout {
  private readonly ImageView _noSegmentIcon;

  public PersonThumbV(Context context, PersonM person) : base(context) {
    _noSegmentIcon = new(context);
    _noSegmentIcon.SetImageResource(Resource.Drawable.icon_people);

    AddView(_noSegmentIcon, new LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.Center });
  }
}
