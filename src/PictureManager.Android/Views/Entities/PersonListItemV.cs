using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.Person;

namespace PictureManager.Android.Views.Entities;

public sealed class PersonListItemV : LinearLayout {
  private readonly TextView _name;

  public PersonListItemV(Context context, PersonM person) : base(context) {
    Orientation = Orientation.Horizontal;

    AddView(new IconView(context).Bind(Resource.Drawable.icon_people, Resource.Color.colorPeople),
      new LayoutParams(DimensU.IconSize, DimensU.IconSize) { Gravity = GravityFlags.Center }.WithDpMargin(DimensU.Spacing));

    AddView(_name = new TextView(context),
      new LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.Center }.WithMargin(DimensU.Spacing, 0, 0, 0));

    this.Bind(person, nameof(PersonM.Name), x => x.Name, (t, p) => t._name.Text = p);
  }
}
