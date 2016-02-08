using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Data {
  public class Ratings: BaseItem {
    public ObservableCollection<Rating> Items { get; set; }

    public Ratings() {
      Items = new ObservableCollection<Rating>();
    }

    public void Load() {
      Items.Clear();
      for (int i = 0; i < 6; i++) {
        Items.Add(new Rating {Value = i, IconName = "appbar_star" });
      }
    }

    public Rating GetRatingByValue(int value) {
      return Items.Single(x => x.Value == value);
    }
  }
}
