namespace PictureManager.ViewModel {
  public sealed class MediaItemSizes : BaseCategoryItem {
    private const int Max = 15000000;
    public MediaItemSize Size = new MediaItemSize {PixelMin = 0, PixelMax = Max };

    public MediaItemSizes() : base(Category.MediaItemSizes) {
      Title = "Sizes";
      IconName = IconName.Bug;
      Items.Add(Size);
    }

    public bool AllSizes() {
      return Size.PixelMin == 0 && Size.PixelMax == Max;
    }

    public bool Fits(int size) {
      return size >= Size.PixelMin && (size <= Size.PixelMax || Size.PixelMax == Max);
    }
  }
}
