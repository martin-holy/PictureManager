using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.ViewModel {
  public class Ratings : BaseTreeViewItem {
    public ObservableCollection<Rating> Items { get; set; }

    public Ratings() {
      Items = new ObservableCollection<Rating>();
      Title = "Ratings";
      IconName = "appbar_star";
    }

    public void Load() {
      Items.Clear();
      for (int i = 0; i < 6; i++) {
        Items.Add(new Rating { Value = i, IconName = "appbar_star" });
      }
    }

    public Rating GetRatingByValue(long value) {
      return Items.Single(x => x.Value == value);
    }
  }
}
