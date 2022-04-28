using PictureManager.Domain.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class MediaItemSizesTreeM : TreeCategoryBase {
    public MediaItemSizeTreeM Size { get; } = new();

    public MediaItemSizesTreeM() : base(Res.IconRuler, Category.MediaItemSizes, "Sizes") {
      Items.Add(Size);
    }
  }
}
