using MH.UI.WPF.BaseClasses;
using PictureManager.Domain;

namespace PictureManager.ViewModels.Tree {
  public sealed class MediaItemSizesTreeVM : CatTreeViewCategoryBase {
    public MediaItemSizeTreeVM Size { get; } = new();
    public RelayCommand<object> RangeChangedCommand { get; }

    public MediaItemSizesTreeVM(ThumbnailsGridsVM tgvm) : base(Category.MediaItemSizes, "Sizes") {
      RangeChangedCommand = new(
        () => {
          tgvm.Model.Current.FilterSize.AllSizes = false;
          _ = tgvm.Model.Current.ReapplyFilter();
        },
        () => tgvm.Model.Current != null);

      Items.Add(Size);
    }
  }
}
