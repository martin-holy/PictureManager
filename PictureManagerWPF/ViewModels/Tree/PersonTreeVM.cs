using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public class PersonTreeVM : CatTreeViewTagItemBase, IFilterItem, IViewModel<PersonM> {
    #region IFilterItem implementation
    private DisplayFilter _displayFilter;
    public DisplayFilter DisplayFilter { get => _displayFilter; set { _displayFilter = value; OnPropertyChanged(); } }
    #endregion

    #region IViewModel<T> implementation
    public PersonM ToModel() => Model;
    #endregion

    public PersonM Model { get; }

    public PersonTreeVM(PersonM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
