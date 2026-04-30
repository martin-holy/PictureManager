using MH.UI.Controls;

namespace PictureManager.Common.Layout;

public sealed class MainTabsSlotVM;

public sealed class MainTabsVM : TabControl {
  public MainTabsVM() : base(new(Dock.Left) { StartSlot = new MainTabsSlotVM(), RotationAngle = 270, JustifyTabSize = true }) {
    CanCloseTabs = true;
    NoTabsText = "Main tabs";
  }
}