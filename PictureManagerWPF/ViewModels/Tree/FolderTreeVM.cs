using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class FolderTreeVM : CatTreeViewItem, ICatTreeViewTagItem {
    public FolderM Model { get; }

    public FolderTreeVM(FolderM model, ICatTreeViewItem parent) {
      Model = model;
      Parent = parent;
    }

    public void UpdateIconName() {
      if (Model.Parent is FolderM && !Model.IsFolderKeyword) // not Drive Folder and not FolderKeyword
        IconName = IsExpanded ? IconName.FolderOpen : IconName.Folder;
    }
  }
}
