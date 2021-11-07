using MH.UI.WPF.Interfaces;
using MH.Utils.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public sealed class CategoryGroupTreeVM : CatTreeViewTagItemBase, ICatTreeViewGroup {
    public CategoryGroupBaseVM BaseVM { get; }

    public CategoryGroupTreeVM(CategoryGroupBaseVM baseVM, ITreeBranch parent) {
      BaseVM = baseVM;
      Parent = parent;
    }
  }
}
