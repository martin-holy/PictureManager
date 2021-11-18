using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public class KeywordTreeVM : CatTreeViewTagItemBase, IFilterItem, IViewModel<KeywordM> {
    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    #region IViewModel<T> implementation
    public KeywordM ToModel() => BaseVM.Model;
    #endregion

    public KeywordBaseVM BaseVM { get; }

    public KeywordTreeVM(KeywordBaseVM baseVM, ITreeBranch parent) {
      BaseVM = baseVM;
      Parent = parent;
    }
  }
}
