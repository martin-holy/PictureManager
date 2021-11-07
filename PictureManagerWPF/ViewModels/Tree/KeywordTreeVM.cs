using MH.Utils.Interfaces;
using PictureManager.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public class KeywordTreeVM : CatTreeViewTagItemBase, IFilterItem {
    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    public KeywordBaseVM BaseVM { get; }

    public KeywordTreeVM(KeywordBaseVM baseVM, ITreeBranch parent) {
      BaseVM = baseVM;
      Parent = parent;
    }
  }
}
