using System;
using System.Windows;

namespace PictureManager.ViewModel {
  public class MediaItemSize : BaseTreeViewTagItem {
    private double _pixelMin;
    private double _pixelMax;
    private double _min;
    private double _max;
    private bool _rangeChanged;

    public double PixelMin {
      get => _pixelMin;
      set {
        _pixelMin = value;
        OnPropertyChanged();
        _rangeChanged = true;
      }
    }

    public double PixelMax {
      get => _pixelMax;
      set {
        _pixelMax = value;
        OnPropertyChanged();
        _rangeChanged = true;
      }
    }

    public double Min { get => _min; set { _min = value; OnPropertyChanged(); } }
    public double Max { get => _max; set { _max = value; OnPropertyChanged(); } }

    public MediaItemSize() {
      IconName = IconName.Bug;
    }

    public bool AllSizes() {
      return !_rangeChanged || (bool?) Application.Current.Properties["MediaItemSizeSliderChanged"] == false;
    }

    public bool Fits(int size) {
      return size >= PixelMin && size <= PixelMax;
    }

    public void SetLoadedRange(int min, int max) {
      PixelMin = Math.Round(min / 1000000.0, 1) * 1000000;
      PixelMax = (Math.Round(max / 1000000.0, 1) + 0.1) * 1000000;
      if ((bool?) Application.Current.Properties["MediaItemSizeSliderChanged"] == true) {
        Application.Current.Properties["MediaItemSizeSliderChanged"] = false;
        return;
      }
      Min = PixelMin;
      Max = PixelMax;
      _rangeChanged = false;
    }
  }
}
