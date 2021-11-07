using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public class FolderTreeVM : CatTreeViewTagItemBase {
    public FolderM Model { get; }

    public FolderTreeVM(FolderM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
