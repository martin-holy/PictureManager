using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public sealed class RatingTreeVM : CatTreeViewItem, ICatTreeViewTagItem, IFilterItem {
    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    public int Value { get; }

    public RatingTreeVM(int value) {
      Value = value;
    }
  }
}
