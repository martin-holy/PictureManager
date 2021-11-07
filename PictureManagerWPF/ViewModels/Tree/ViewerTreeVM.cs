using MH.UI.WPF.BaseClasses;
using MH.Utils.Interfaces;
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
