using MH.UI.WPF.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public class KeywordTreeVM : CatTreeViewItem, IFilterItem, IViewModel<KeywordM> {
    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    #region IViewModel<T> implementation
    public KeywordM ToModel() => Model;
    #endregion

    public KeywordM Model { get; }

    public KeywordTreeVM(KeywordM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
