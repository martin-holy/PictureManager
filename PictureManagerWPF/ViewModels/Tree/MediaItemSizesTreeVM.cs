using PictureManager.Domain;

namespace PictureManager.ViewModels.Tree {
  public sealed class MediaItemSizesTreeVM : BaseCatTreeViewCategory {
    public MediaItemSizeTreeVM Size { get; } = new();

    public MediaItemSizesTreeVM(AppCore appCore) : base(Category.MediaItemSizes) {
      Title = "Sizes";
      IconName = IconName.Ruler;
      Size.RangeChangedEvent += (_, _) => {
        _ = appCore.MediaItemsViewModel.ReapplyFilter();
      };
      Items.Add(Size);
    }
  }
}
