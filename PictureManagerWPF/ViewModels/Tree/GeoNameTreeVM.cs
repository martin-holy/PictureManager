using MH.UI.WPF.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public class GeoNameTreeVM : CatTreeViewItem, IFilterItem, IViewModel<GeoNameM> {
    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    #region IViewModel<T> implementation
    public GeoNameM ToModel() => Model;
    #endregion

    public GeoNameM Model { get; }

    public GeoNameTreeVM(GeoNameM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
