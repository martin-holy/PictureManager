using MH.UI.Controls;

namespace MH.UI.WPF.Sample.ViewModels.Layout;

public class LeftContentVM : TabControl {
  public LeftContentVM() : base(new(Dock.Left, Dock.Top, new SlidePanelPinButton()) { JustifyTabSize = true }) {
    CanCloseTabs = true;
  }
}