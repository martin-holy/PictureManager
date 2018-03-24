namespace PictureManager.ViewModel {
  public class MediaItemSize : BaseTreeViewTagItem {
    private double _pixelMin;
    private double _pixelMax;
    public double PixelMin { get => _pixelMin; set { _pixelMin = value; OnPropertyChanged(); } }
    public double PixelMax { get => _pixelMax; set { _pixelMax = value; OnPropertyChanged(); } }

    public MediaItemSize() {
      IconName = IconName.Bug;
    }
  }
}
