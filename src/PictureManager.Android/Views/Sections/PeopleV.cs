using Android.Content;
using Android.Views;
using Android.Widget;
using MH.UI.Android.Controls;
using MH.UI.Interfaces;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.Person;

namespace PictureManager.Android.Views.Sections;

public sealed class PeopleV : FrameLayout {
  public PeopleVM DataContext { get; }

  public PeopleV(Context context, PeopleVM dataContext) : base(context) {
    DataContext = dataContext;
    SetBackgroundResource(Resource.Color.c_static_ba);
    AddView(new CollectionViewHost(context, dataContext, GetPersonV));
  }

  // TODO group.ViewModes
  public static View? GetPersonV(LinearLayout container, ICollectionViewGroup group, object? item) {
    if (item is not PersonM person) return null;
    return group.ViewMode switch {
      _ => new PersonThumbV(container.Context!, person)
    };
  }
}