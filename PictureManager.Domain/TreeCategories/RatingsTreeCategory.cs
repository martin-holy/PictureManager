using MH.UI.BaseClasses;
using PictureManager.Domain.Models;
using System.Linq;

namespace PictureManager.Domain.TreeCategories;

public sealed class RatingsTreeCategory : TreeCategory {
  public RatingsTreeCategory() : base(Res.IconStar, "Ratings", (int)Category.Ratings) {
    Load();
  }

  public override void OnItemSelected(object o) {
    if (o is not RatingTreeM r) return;
    if (Core.MediaItemsM.IsEditModeOn)
      Core.MediaItemsM.SetMetadata(r);
    else
      Core.MediaItemsViews.Current?.Filter.Set(r.Rating, DisplayFilter.Or);
  }

  private void Load() {
    for (var i = 0; i < 6; i++)
      Items.Add(new RatingTreeM(i));
  }

  public RatingTreeM GetRatingByValue(int value) =>
    Items.Cast<RatingTreeM>().Single(x => x.Rating.Value == value);
}