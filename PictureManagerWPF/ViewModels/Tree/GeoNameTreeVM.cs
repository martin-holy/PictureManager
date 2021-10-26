using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public class GeoNameTreeVM : CatTreeViewItem, ICatTreeViewTagItem {
    public GeoNameM Model { get; }

    public GeoNameTreeVM(GeoNameM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
