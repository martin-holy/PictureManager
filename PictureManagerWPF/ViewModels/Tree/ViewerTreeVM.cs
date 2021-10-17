using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public class ViewerTreeVM : CatTreeViewItem {
    public ViewerM Model { get; }

    public ViewerTreeVM(ViewerM model, ICatTreeViewItem parent) {
      Model = model;
      Parent = parent;
    }
  }
}
