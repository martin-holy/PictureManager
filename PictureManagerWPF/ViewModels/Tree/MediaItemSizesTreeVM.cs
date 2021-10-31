using PictureManager.Domain;

namespace PictureManager.ViewModels.Tree {
  public sealed class MediaItemSizesTreeVM : BaseCatTreeViewCategory {
    public MediaItemSizeTreeVM Size { get; } = new();

    public MediaItemSizesTreeVM(AppCore coreVM) : base(Category.MediaItemSizes) {
      Name = "Sizes";

      Size.RangeChangedEvent += (_, _) => {
        _ = coreVM.MediaItemsViewModel.ReapplyFilter();
      };

      Items.Add(Size);
    }
  }
}
