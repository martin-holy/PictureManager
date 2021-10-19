using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class FolderKeywordTreeVM : CatTreeViewItem, ICatTreeViewTagItem {
    public FolderKeywordM Model { get; }

    public FolderKeywordTreeVM(FolderKeywordM model, ICatTreeViewItem parent) {
      Model = model;
      Parent = parent;
      OnExpand += (_, _) => Model.Folders.ForEach(x => x.LoadSubFolders(false));
    }
  }
}
