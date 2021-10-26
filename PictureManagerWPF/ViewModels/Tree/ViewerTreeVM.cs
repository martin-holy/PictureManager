using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public class ViewerTreeVM : CatTreeViewItem {
    public ViewerM Model { get; }

    public ViewerTreeVM(ViewerM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
