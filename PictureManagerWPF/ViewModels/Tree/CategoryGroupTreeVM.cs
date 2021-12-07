using MH.UI.WPF.Interfaces;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class CategoryGroupTreeVM : CatTreeViewTagItemBase, ICatTreeViewGroup {
    public CategoryGroupM Model { get; }
    public new bool IsHidden { get => Model.IsHidden; set => Model.IsHidden = value; }

    public CategoryGroupTreeVM(CategoryGroupM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;

      Model.PropertyChanged += (_, e) => {
        if (nameof(Model.IsHidden).Equals(e.PropertyName))
          OnPropertyChanged(nameof(IsHidden));
      };
    }
  }
}
