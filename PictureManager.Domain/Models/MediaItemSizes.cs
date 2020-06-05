namespace PictureManager.Domain.Models {
  public sealed class MediaItemSizes : BaseCategoryItem {
    public MediaItemSize Size { get; } = new MediaItemSize();

    public MediaItemSizes() : base(Category.MediaItemSizes) {
      Title = "Sizes";
      IconName = IconName.Ruler;
      Items.Add(Size);
    }
  }
}
