using MH.UI.Controls;

namespace MH.UI.WPF.Sample.ViewModels.Layout;

public class LeftContentVM : TabControl {
  public LeftContentVM() {
    CanCloseTabs = true;
    TabStrip = new() {
      Placement = Dock.Left,
      Slot = new SlidePanelPinButton(),
      SlotPlacement = Dock.Top,
      RotationAngle = 0,
      JustifyTabSize = true
    };
  }
}