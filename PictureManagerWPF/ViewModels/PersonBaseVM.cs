using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class PersonBaseVM : ObservableObject, ITreeLeaf {
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
