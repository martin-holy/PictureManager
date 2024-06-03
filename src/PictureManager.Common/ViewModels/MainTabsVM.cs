using MH.UI.Controls;

namespace PictureManager.Common.ViewModels;

public sealed class MainTabsSlotVM;

public sealed class MainTabsVM : TabControl {
  public MainTabsVM() {
    CanCloseTabs = true;
    TabStrip = new() {
      Placement = Dock.Left,
      Slot = new MainTabsSlotVM(),
      SlotPlacement = Dock.Top,
      RotationAngle = 270,
      JustifyTabSize = true
    };
  }
}