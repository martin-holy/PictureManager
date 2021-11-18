using PictureManager.Domain;
using PictureManager.Domain.Interfaces;
using PictureManager.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public sealed class RatingTreeVM : CatTreeViewTagItemBase, IFilterItem, IViewModel<int> {
    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    #region IViewModel<T> implementation
    public int ToModel() => Value;
    #endregion

    public int Value { get; }

    public RatingTreeVM(int value) {
      Value = value;
    }
  }
}
