using MH.Utils.BaseClasses;

namespace PictureManager.Domain.ViewModels;

public sealed class TitleProgressBarVM : ObservableObject {
  private int _valueA = 100;
  private int _valueB = 100;
  private int _maxA = 100;
  private int _maxB = 100;
  private bool _isIndeterminate;
  private bool _isVisible;

  public int ValueA { get => _valueA; set { _valueA = value; OnPropertyChanged(); } }
  public int ValueB { get => _valueB; set { _valueB = value; OnPropertyChanged(); } }
  public int MaxA { get => _maxA; set { _maxA = value; OnPropertyChanged(); } }
  public int MaxB { get => _maxB; set { _maxB = value; OnPropertyChanged(); } }
  public bool IsIndeterminate { get => _isIndeterminate; set { _isIndeterminate = value; OnPropertyChanged(); } }
  public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }

  public void ResetProgressBars(int max) {
    ResetProgressBarA(max);
    ResetProgressBarB(max);
  }

  public void ResetProgressBarA(int max) {
    ValueA = 0;
    MaxA = max;
  }

  public void ResetProgressBarB(int max) {
    ValueB = 0;
    MaxB = max;
  }
}