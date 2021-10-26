using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public class KeywordTreeVM : CatTreeViewItem, ICatTreeViewTagItem {
    public KeywordBaseVM BaseVM { get; }

    public KeywordTreeVM(KeywordBaseVM baseVM, ITreeBranch parent) {
      BaseVM = baseVM;
      Parent = parent;
    }
  }
}
