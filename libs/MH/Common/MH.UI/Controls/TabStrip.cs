using MH.Utils.BaseClasses;

namespace MH.UI.Controls;

public sealed class TabStrip : ObservableObject {
  private Dock _placement;
  private Dock _slotPlacement;
  private object _slot;
  private int _rotationAngle;
  private bool _justifyTabSize;
  private double _maxTabWidth;
  private double _maxTabHeight;
  private double _size;

  public Dock Placement { get => _placement; set { _placement = value; OnPropertyChanged(); } }
  public Dock SlotPlacement { get => _slotPlacement; set { _slotPlacement = value; OnPropertyChanged(); } }
  public object Slot { get => _slot; set { _slot = value; OnPropertyChanged(); } }
  public int RotationAngle { get => _rotationAngle; set { _rotationAngle = value; OnPropertyChanged(); } }
  public bool JustifyTabSize { get => _justifyTabSize; set { _justifyTabSize = value; OnPropertyChanged(); } }
  public double MaxTabWidth { get => _maxTabWidth; set { _maxTabWidth = value; OnPropertyChanged(); } }
  public double MaxTabHeight { get => _maxTabHeight; set { _maxTabHeight = value; OnPropertyChanged(); } }
  public double Size { get => _size; set { _size = value; OnPropertyChanged(); } }

  public void UpdateMaxTabSize(int tabsCount) {
    if (tabsCount == 0 || !JustifyTabSize) {
      ResetMaxTabSize();
      return;
    }

    if (Placement is Dock.Top or Dock.Bottom) {
      if (RotationAngle == 0)
        ResetMaxTabSize((int)(Size / tabsCount));
      else
        ResetMaxTabSize();
    }
    else {
      if (RotationAngle == 0)
        ResetMaxTabSize();
      else
        ResetMaxTabSize(int.MaxValue, (int)(Size / tabsCount));
    }
  }

  public void UpdateMaxTabSize(double? width, double? height, int tabsCount) {
    if (!JustifyTabSize || width is not { } w || height is not { } h) {
      ResetMaxTabSize();
      return;
    }

    Size = Placement is Dock.Top or Dock.Bottom ? w : h;
    UpdateMaxTabSize(tabsCount);
  }

  private void ResetMaxTabSize(int w = int.MaxValue, int h = int.MaxValue) {
    MaxTabWidth = w;
    MaxTabHeight = h;
  }
}