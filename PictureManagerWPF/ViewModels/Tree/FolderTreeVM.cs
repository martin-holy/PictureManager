using MH.UI.WPF.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public class FolderTreeVM : CatTreeViewItem {
    public FolderM Model { get; }

    public FolderTreeVM(FolderM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
