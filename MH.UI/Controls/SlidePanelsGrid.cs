using MH.Utils.BaseClasses;

namespace MH.UI.Controls;

public enum Position { Left, Top, Right, Bottom }

public class SlidePanelsGrid : ObservableObject {
  private int _activeLayout;
  private double _panelTopGridHeight;
  private double _panelLeftGridWidth;
  private double _panelRightGridWidth;
  private double _panelBottomGridHeight;

  public int ActiveLayout { get => _activeLayout; set => OnActivateLayoutChanged(value); }
  public bool[][] PinLayouts { get; }
  public SlidePanel PanelLeft { get; }
  public SlidePanel PanelTop { get; }
  public SlidePanel PanelRight { get; }
  public SlidePanel PanelBottom { get; }
  public object PanelMiddle { get; }
  public double PanelTopGridHeight { get => _panelTopGridHeight; set => SetIfVary(ref _panelTopGridHeight, value); }
  public double PanelBottomGridHeight { get => _panelBottomGridHeight; set => SetIfVary(ref _panelBottomGridHeight, value); }

  public double PanelLeftGridWidth {
    get => _panelLeftGridWidth;
    set {
      if (SetIfVary(ref _panelLeftGridWidth, value))
        PanelLeft?.SetSize(value);
    }
  }

  public double PanelRightGridWidth {
    get => _panelRightGridWidth;
    set {
      if (SetIfVary(ref _panelRightGridWidth, value))
        PanelRight?.SetSize(value);
    }
  }

  public SlidePanelsGrid(SlidePanel left, SlidePanel top, SlidePanel right, SlidePanel bottom, object middle, bool[][] pinLayouts) {
    PanelLeft = left;
    PanelTop = top;
    PanelRight = right;
    PanelBottom = bottom;
    PanelMiddle = middle;
    PinLayouts = pinLayouts;
    PanelLeftGridWidth = PanelLeft?.Size ?? 0;
    PanelRightGridWidth = PanelRight?.Size ?? 0;
    ActiveLayout = 0;
    InitPanel(PanelLeft);
    InitPanel(PanelTop);
    InitPanel(PanelRight);
    InitPanel(PanelBottom);
  }

  private void InitPanel(SlidePanel panel) {
    if (panel == null) return;
    panel.PropertyChanged += (_, e) => {
      if (!nameof(panel.IsPinned).Equals(e.PropertyName)) return;
      PinLayouts[ActiveLayout][(int)panel.Position] = panel.IsPinned;
      SetPin(panel);
    };
  }

  private void OnActivateLayoutChanged(int value) {
    _activeLayout = value;
    OnPropertyChanged(nameof(ActiveLayout));
    var activeLayout = PinLayouts[value];
    if (PanelLeft != null) PanelLeft.IsPinned = activeLayout[0];
    if (PanelTop != null) PanelTop.IsPinned = activeLayout[1];
    if (PanelRight != null) PanelRight.IsPinned = activeLayout[2];
    if (PanelBottom != null) PanelBottom.IsPinned = activeLayout[3];
  }

  public void SetPin(SlidePanel panel) {
    var size = panel.IsPinned ? panel.Size : 0;
    if (ReferenceEquals(panel, PanelLeft)) PanelLeftGridWidth = size;
    else if (ReferenceEquals(panel, PanelTop)) PanelTopGridHeight = size;
    else if (ReferenceEquals(panel, PanelRight)) PanelRightGridWidth = size;
    else if (ReferenceEquals(panel, PanelBottom)) PanelBottomGridHeight = size;
  }

  public void OnMouseMove(double posX, double posY, double width, double height) {
    // to stop opening/closing panel by it self in some cases
    if ((posX == 0 && posY == 0) || posX < 0 || posY < 0) return;
    PanelLeft?.OnMouseMove(size => posX > size, posX < 5);
    PanelTop?.OnMouseMove(size => posY > size, posY < 5);
    PanelRight?.OnMouseMove(size => posX < width - size, posX > width - 5);
    PanelBottom?.OnMouseMove(size => posY < height - size, posY > height - 5);
  }
}