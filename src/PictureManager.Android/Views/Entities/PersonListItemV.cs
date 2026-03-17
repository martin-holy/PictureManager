using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.Person;
using System;

namespace PictureManager.Android.Views.Entities;

public sealed class PersonListItemV : LinearLayout, ICollectionViewItemContent {
  private readonly TextView _name;
  private IDisposable? _personNameBinding;

  public object? DataContext { get; private set; }
  public View View => this;

  public PersonListItemV(Context context) : base(context) {
    Orientation = Orientation.Horizontal;

    var icon = new IconView(context).Bind(Resource.Drawable.icon_people, Resource.Color.colorPeople);
    _name = new TextView(context);

    AddView(icon, LPU.Linear(DimensU.IconSize, DimensU.IconSize, GravityFlags.Center).WithDpMargin(DimensU.Spacing));
    AddView(_name, LPU.Linear(LPU.Wrap, LPU.Wrap, GravityFlags.Center).WithMargin(DimensU.Spacing, 0, 0, 0));
  }

  public void Bind(object item) {
    DataContext = item;
    if (item is not PersonM person) return;
    _personNameBinding = person.Bind(nameof(PersonM.Name), x => x.Name, x => _name.Text = x);
  }

  public void Unbind() {
    _personNameBinding?.Dispose();
    _personNameBinding = null;
  }
}