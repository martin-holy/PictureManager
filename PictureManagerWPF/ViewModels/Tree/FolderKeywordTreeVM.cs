using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class FolderKeywordTreeVM : CatTreeViewTagItemBase {
    public FolderKeywordM Model { get; }

    public FolderKeywordTreeVM(FolderKeywordM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
