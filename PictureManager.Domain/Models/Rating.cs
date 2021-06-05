using PictureManager.Domain.CatTreeViewModels;

namespace PictureManager.Domain.Models {
  public class Rating : CatTreeViewItem, ICatTreeViewTagItem {
    public int Value { get; set; }

    public Rating() {
      IconName = IconName.Star;
    }
  }
}
