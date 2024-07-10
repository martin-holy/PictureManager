using MH.UI.Controls;

namespace PictureManager.Common.ViewModels;

public sealed class MainTabsSlotVM;

public sealed class MainTabsVM : TabControl {
  public MainTabsVM() : base(new(Dock.Left, Dock.Top, new MainTabsSlotVM()) { RotationAngle = 270, JustifyTabSize = true }) {
    CanCloseTabs = true;
  }
}