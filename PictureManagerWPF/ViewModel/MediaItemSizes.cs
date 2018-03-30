namespace PictureManager.ViewModel {
  public sealed class MediaItemSizes : BaseCategoryItem {
    public MediaItemSize Size = new MediaItemSize();

    public MediaItemSizes() : base(Category.MediaItemSizes) {
      Title = "Sizes";
      IconName = IconName.Bug;
      Items.Add(Size);
    }
  }
}
