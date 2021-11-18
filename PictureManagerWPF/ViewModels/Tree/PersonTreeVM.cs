using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public class PersonTreeVM : CatTreeViewTagItemBase, IFilterItem, IViewModel<PersonM> {
    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    #region IViewModel<T> implementation
    public PersonM ToModel() => BaseVM.Model;
    #endregion

    public PersonBaseVM BaseVM { get; }

    public PersonTreeVM(PersonBaseVM baseVM, ITreeBranch parent) {
      BaseVM = baseVM;
      Parent = parent;
    }
  }
}
