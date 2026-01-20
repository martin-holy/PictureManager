using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using PictureManager.Common;
using PictureManager.Common.Features.Rating;

namespace PictureManager.Android.Views.Entities;

public sealed class RatingV : LinearLayout {
  public RatingV(Context context, RatingM dataContext) : base(context) {
    Orientation = Orientation.Horizontal;

    AddView(new IconView(context, Res.IconStar, _getColor(0, dataContext)));
    AddView(new IconView(context, Res.IconStar, _getColor(1, dataContext)));
    AddView(new IconView(context, Res.IconStar, _getColor(2, dataContext)));
    AddView(new IconView(context, Res.IconStar, _getColor(3, dataContext)));
    AddView(new IconView(context, Res.IconStar, _getColor(4, dataContext)));
  }

  private static int _getColor(int position, RatingM rating) =>
    position < rating.Value
      ? Resource.Color.c_white
      : Resource.Color.gray4;
}