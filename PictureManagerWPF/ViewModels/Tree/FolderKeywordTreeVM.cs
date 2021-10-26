using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class FolderKeywordTreeVM : CatTreeViewItem, ICatTreeViewTagItem {
    public FolderKeywordM Model { get; }

    public FolderKeywordTreeVM(FolderKeywordM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
