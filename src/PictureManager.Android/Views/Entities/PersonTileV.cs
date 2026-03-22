using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Android.Extensions;
using MH.UI.Android.Utils;
using MH.Utils;
using PictureManager.Common.Features.Person;
using System;

namespace PictureManager.Android.Views.Entities;

public sealed class PersonTileV : LinearLayout, ICollectionViewItemContent {
  private readonly PersonThumbV _personThumbV;
  private readonly TextView _name;
  private IDisposable? _personNameBinding;

  public object? DataContext { get; private set; }
  public View View => this;

  public PersonTileV(Context context) : base(context) {
    Orientation = Orientation.Horizontal;
    _personThumbV = new PersonThumbV(context);
    _name = new TextView(context);
    AddView(_personThumbV, LPU.Linear(PersonVM.PersonTileSegmentWidth, PersonVM.PersonTileSegmentWidth));
    AddView(_name, LPU.LinearWrap(GravityFlags.Center).WithMargin(DimensU.Spacing, 0, 0, 0));
  }

  public void Bind(object item) {
    DataContext = item;
    if (item is not PersonM person) return;
    _personThumbV.Bind(person);
    _personNameBinding = person.Bind(nameof(PersonM.Name), x => x.Name, x => _name.Text = x);
  }

  public void Unbind() {
    _personThumbV.Unbind();
    _personNameBinding?.Dispose();
    _personNameBinding = null;
  }
}