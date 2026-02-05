using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.Person;
using System;

namespace PictureManager.Android.Views.Entities;

public sealed class PersonListItemV : LinearLayout, ICollectionViewItemContent {
  private readonly TextView _name;
  private IDisposable? _personNameBinding;

  public View View => this;

  public PersonListItemV(Context context) : base(context) {
    Orientation = Orientation.Horizontal;

    AddView(new IconView(context).Bind(Resource.Drawable.icon_people, Resource.Color.colorPeople),
      new LayoutParams(DimensU.IconSize, DimensU.IconSize) { Gravity = GravityFlags.Center }.WithDpMargin(DimensU.Spacing));

    AddView(_name = new TextView(context),
      new LayoutParams(LPU.Wrap, LPU.Wrap) { Gravity = GravityFlags.Center }.WithMargin(DimensU.Spacing, 0, 0, 0));
  }

  public void Bind(object item) {
    if (item is not PersonM person) return;
    _personNameBinding?.Dispose();
    _personNameBinding = this.Bind(person, nameof(PersonM.Name), x => x.Name, (t, p) => t._name.Text = p);
  }

  public void Unbind() {
    _personNameBinding?.Dispose();
    _personNameBinding = null;
  }
}