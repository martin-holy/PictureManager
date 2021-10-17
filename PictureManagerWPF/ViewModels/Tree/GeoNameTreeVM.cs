using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public class GeoNameTreeVM : CatTreeViewItem, ICatTreeViewTagItem {
    public GeoNameM Model { get; }

    public GeoNameTreeVM(GeoNameM model, ICatTreeViewItem parent) {
      Model = model;
      Parent = parent;
    }
  }
}
