using System;
using MH.UI.WPF.BaseClasses;

namespace PictureManager.ViewModels.Tree {
  public sealed class MediaItemSizeTreeVM : CatTreeViewItem {
    private double _pixelMin;
    private double _pixelMax;
    private double _min;
    private double _max;
    private bool _sliderChanged;

    public double PixelMin { get => _pixelMin; set { _pixelMin = value; OnPropertyChanged(); } }
    public double PixelMax { get => _pixelMax; set { _pixelMax = value; OnPropertyChanged(); } }
    public double Min { get => _min; set { _min = value; OnPropertyChanged(); } }
    public double Max { get => _max; set { _max = value; OnPropertyChanged(); } }
    
    public bool AllSizes() => !_sliderChanged;
    public bool Fits(int size) => size >= PixelMin && size <= PixelMax;
    public RelayCommand<object> RangeChangedCommand { get; }
    public event EventHandler RangeChangedEvent = delegate { };

    public MediaItemSizeTreeVM() {
      RangeChangedCommand = new(RangeChanged);
    }

    public void SetLoadedRange(int min, int max) {
      PixelMin = Math.Round(min / 1000000.0, 1) * 1000000;
      PixelMax = Math.Round((max + 100000) / 1000000.0, 1) * 1000000;
      if (_sliderChanged) {
        _sliderChanged = false;
        return;
      }
      Min = PixelMin;
      Max = PixelMax;
    }

    private void RangeChanged() {
      _sliderChanged = true;
      RangeChangedEvent(this, EventArgs.Empty);
    }
  }
}
