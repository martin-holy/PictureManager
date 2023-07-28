using System.Windows.Controls;

namespace MH.UI.WPF.Controls {
  public class VirtualizingSingleItemScrollStackPanel : VirtualizingStackPanel {
    public override void MouseWheelDown() => LineDown();
    public override void MouseWheelUp() => LineUp();
  }
}
