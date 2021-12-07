using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class PersonBaseVM : ObservableObject, ISelectable, ITreeLeaf {
    #region ISelectable implementation
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    #endregion

    #region ITreeLeaf implementation
    public ITreeBranch Parent { get; set; }
    #endregion

    public PersonM Model { get; }

    public PersonBaseVM(PersonM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
