using MH.Utils.Interfaces;
using PictureManager.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public class PersonTreeVM : CatTreeViewTagItemBase, IFilterItem {
    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    public PersonBaseVM BaseVM { get; }

    public PersonTreeVM(PersonBaseVM baseVM, ITreeBranch parent) {
      BaseVM = baseVM;
      Parent = parent;
    }
  }
}
