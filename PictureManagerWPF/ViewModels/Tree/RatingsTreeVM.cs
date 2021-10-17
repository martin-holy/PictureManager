using System.Linq;
using PictureManager.Domain;

namespace PictureManager.ViewModels.Tree {
  public sealed class RatingsTreeVM : BaseCatTreeViewCategory {
    public RatingsTreeVM() : base(Category.Ratings) {
      Title = "Ratings";
      IconName = IconName.Star;

      Load();
    }

    private void Load() {
      for (var i = 0; i < 6; i++)
        Items.Add(new RatingTreeVM(i));
    }

    public RatingTreeVM GetRatingByValue(int value) => Items.Cast<RatingTreeVM>().Single(x => x.Value == value);
  }
}
