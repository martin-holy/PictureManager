using PictureManager.Domain.CatTreeViewModels;

namespace PictureManager.ViewModels.Tree {
  public sealed class RatingTreeVM : CatTreeViewItem, ICatTreeViewTagItem {
    public int Value { get; }

    public RatingTreeVM(int value) {
      Value = value;
    }
  }
}
