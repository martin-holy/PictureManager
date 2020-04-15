namespace PictureManager.ViewModel {
  public sealed class MediaItemSizes : BaseCategoryItem {
    public MediaItemSize Size { get; } = new MediaItemSize();

    public MediaItemSizes() : base(Category.MediaItemSizes) {
      Title = "Sizes";
      IconName = IconName.Bug;
      Items.Add(Size);
    }
  }
}
