using MH.UI.Controls;

namespace MH.UI.WPF.Sample.ViewModels.Layout;

public sealed class MiddleContentSlotVM;

public sealed class MiddleContentVM : TabControl {
  public MiddleContentVM() {
    TabStrip = new() {
      Placement = Dock.Left,
      Slot = new MiddleContentSlotVM(),
      SlotPlacement = Dock.Top,
      RotationAngle = 270
    };
  }
}