using PictureManager.Domain.BaseClasses;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class RatingsTreeM : TreeCategoryBase {
    public RatingsTreeM() : base(Res.IconStar, Category.Ratings, "Ratings") {
      Load();
    }

    private void Load() {
      for (var i = 0; i < 6; i++)
        Items.Add(new RatingTreeM(i));
    }

    public RatingTreeM GetRatingByValue(int value) =>
      Items.Cast<RatingTreeM>().Single(x => x.Value == value);
  }
}
