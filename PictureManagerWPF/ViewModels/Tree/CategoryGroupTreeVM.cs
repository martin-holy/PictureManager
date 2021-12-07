using MH.UI.WPF.Interfaces;
using MH.Utils.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public sealed class CategoryGroupTreeVM : CatTreeViewTagItemBase, ICatTreeViewGroup {
    public CategoryGroupBaseVM BaseVM { get; }
    public new bool IsHidden { get => BaseVM.Model.IsHidden; set => BaseVM.Model.IsHidden = value; }

    public CategoryGroupTreeVM(CategoryGroupBaseVM baseVM, ITreeBranch parent) {
      BaseVM = baseVM;
      Parent = parent;

      BaseVM.Model.PropertyChanged += (_, e) => {
        if (nameof(BaseVM.Model.IsHidden).Equals(e.PropertyName))
          OnPropertyChanged(nameof(IsHidden));
      };
    }
  }
}
