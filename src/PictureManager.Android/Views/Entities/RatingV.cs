using Android.Content;
using Android.Widget;
using MH.UI.Android.Controls;
using PictureManager.Common;
using PictureManager.Common.Features.Rating;

namespace PictureManager.Android.Views.Entities;

public sealed class RatingV : LinearLayout {
  public RatingV(Context context) : base(context) {
    Orientation = Orientation.Horizontal;

    AddView(new IconView(context));
    AddView(new IconView(context));
    AddView(new IconView(context));
    AddView(new IconView(context));
    AddView(new IconView(context));
  }

  public void Bind(RatingM rating) {
    for (int i = 0; i < 5; i++)
      ((IconView)GetChildAt(i)!).Bind(Res.IconStar, _getColor(i, rating));
  }

  private static int _getColor(int position, RatingM rating) =>
    position < rating.Value
      ? Resource.Color.c_white
      : Resource.Color.gray4;
}