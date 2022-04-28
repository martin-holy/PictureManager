using MH.Utils.BaseClasses;
using PictureManager.Domain.Interfaces;

namespace PictureManager.Domain.Models {
  public sealed class RatingTreeM : TreeItem, IFilterItem {
    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    public int Value { get; }

    public RatingTreeM(int value) {
      Value = value;
    }
  }
}
