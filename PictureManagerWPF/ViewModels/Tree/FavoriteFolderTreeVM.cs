using MH.UI.WPF.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class FavoriteFolderTreeVM : CatTreeViewItem {
    public FavoriteFolderM Model { get; set; }

    public FavoriteFolderTreeVM(FavoriteFolderM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
