using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class FavoriteFolderTreeVM : CatTreeViewItem {
    public FavoriteFolderM Model { get; set; }

    public FavoriteFolderTreeVM(FavoriteFolderM model, ICatTreeViewItem parent) {
      Model = model;
      Parent = parent;
    }
  }
}
