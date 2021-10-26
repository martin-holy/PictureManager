using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public sealed class CategoryGroupTreeVM : CatTreeViewItem, ICatTreeViewGroup, ICatTreeViewTagItem {
    public CategoryGroupBaseVM BaseVM { get; }

    public CategoryGroupTreeVM(CategoryGroupBaseVM baseVM, ITreeBranch parent) {
      BaseVM = baseVM;
      Parent = parent;
    }
  }
}
