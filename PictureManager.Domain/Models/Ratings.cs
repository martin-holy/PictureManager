using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class Ratings : BaseCategoryItem {

    public Ratings() : base(Category.Ratings) {
      Title = "Ratings";
      IconName = IconName.Star;
    }

    public void Load() {
      Items.Clear();
      for (var i = 0; i < 6; i++)
        Items.Add(new Rating {Value = i});
    }

    public Rating GetRatingByValue(int value) {
      return Items.Cast<Rating>().Single(x => x.Value == value);
    }
  }
}
