using System.Linq;

namespace PictureManager.ViewModel {
  public class Ratings : BaseCategoryItem {

    public Ratings() : base(Categories.Ratings) {
      Title = "Ratings";
      IconName = "appbar_star";
    }

    public void Load() {
      Items.Clear();
      for (int i = 0; i < 6; i++) {
        Items.Add(new Rating {Value = i});
      }
    }

    public Rating GetRatingByValue(int value) {
      return Items.Cast<Rating>().Single(x => x.Value == value);
    }
  }
}
