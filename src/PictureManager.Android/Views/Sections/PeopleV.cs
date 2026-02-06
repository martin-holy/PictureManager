using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls.Hosts.CollectionViewHost;
using MH.UI.Controls;
using MH.UI.Interfaces;
using PictureManager.Android.Views.Entities;
using PictureManager.Common.Features.Person;

namespace PictureManager.Android.Views.Sections;

public sealed class PeopleV : FrameLayout {
  public PeopleVM DataContext { get; }

  public PeopleV(Context context, PeopleVM dataContext) : base(context) {
    DataContext = dataContext;
    SetBackgroundResource(Resource.Color.c_static_ba);
    AddView(new CollectionViewHost(context, dataContext, CreatePersonV));
  }

  public static ICollectionViewItemContent CreatePersonV(Context context, ICollectionViewGroup group) =>
    group.ViewMode switch {
      CollectionView.ViewMode.ThumbSmall => new PersonThumbV(context),
      CollectionView.ViewMode.List => new PersonListItemV(context),
      CollectionView.ViewMode.Tiles => new PersonTileV(context),
      _ => new PersonThumbV(context)
    };
}