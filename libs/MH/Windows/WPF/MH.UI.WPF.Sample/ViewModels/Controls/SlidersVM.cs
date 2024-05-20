using MH.Utils.BaseClasses;

namespace MH.UI.WPF.Sample.ViewModels.Controls;

public class SlidersVM : ObservableObject {
  private double _popupSliderValue = 0.5;

  public double PopupSliderValue { get => _popupSliderValue; set { _popupSliderValue = value; OnPropertyChanged(); } }
  public SelectionRange SelRangeA { get; } = new();
  public SelectionRange SelRangeB { get; } = new();

  public SlidersVM() {
    SelRangeA.Reset(0.1, 20);
    SelRangeA.Start = 5.2;
    SelRangeA.End = 10;
    SelRangeB.Reset(1, 100);
    SelRangeB.Start = 30;
    SelRangeB.End = 80;
  }
}