using PictureManager.Domain.BaseClasses;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class RatingsTreeM : TreeCategoryBase {
    public RatingsTreeM() : base(Res.IconStar, Category.Ratings, "Ratings") {
      Load();
    }

    public override void OnItemSelect(object o) {
      if (o is not RatingTreeM r) return;
      if (Core.Instance.MediaItemsM.IsEditModeOn)
        Core.Instance.MediaItemsM.SetMetadata(r);
      else
        Core.Instance.MediaItemsViews.Current?.Filter.Set(r.Rating, DisplayFilter.Or);
    }

    private void Load() {
      for (var i = 0; i < 6; i++)
        Items.Add(new RatingTreeM(i));
    }

    public RatingTreeM GetRatingByValue(int value) =>
      Items.Cast<RatingTreeM>().Single(x => x.Rating.Value == value);
  }
}
