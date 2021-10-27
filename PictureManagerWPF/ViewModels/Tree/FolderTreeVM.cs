using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public class FolderTreeVM : CatTreeViewItem, ICatTreeViewTagItem {
    public FolderM Model { get; }

    public FolderTreeVM(FolderM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
