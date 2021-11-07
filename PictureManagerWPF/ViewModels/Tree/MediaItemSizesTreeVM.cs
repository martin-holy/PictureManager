using PictureManager.Domain;

namespace PictureManager.ViewModels.Tree {
  public sealed class MediaItemSizesTreeVM : CatTreeViewCategoryBase {
    public MediaItemSizeTreeVM Size { get; } = new();

    public MediaItemSizesTreeVM(AppCore coreVM) : base(Category.MediaItemSizes, "Sizes") {
      Size.RangeChangedEvent += (_, _) => {
        _ = coreVM.MediaItemsViewModel.ReapplyFilter();
      };

      Items.Add(Size);
    }
  }
}
