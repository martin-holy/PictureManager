using PictureManager.Domain.CatTreeViewModels;

namespace PictureManager.ViewModels.Tree {
  public sealed class CategoryGroupTreeVM : CatTreeViewItem, ICatTreeViewGroup, ICatTreeViewTagItem {
    public CategoryGroupBaseVM BaseVM { get; }

    public CategoryGroupTreeVM(CategoryGroupBaseVM baseVM, ICatTreeViewItem parent) {
      BaseVM = baseVM;
      Parent = parent;
    }
  }
}
