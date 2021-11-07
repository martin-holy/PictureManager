using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public class GeoNameTreeVM : CatTreeViewTagItemBase, IFilterItem {
    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    public GeoNameM Model { get; }

    public GeoNameTreeVM(GeoNameM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
