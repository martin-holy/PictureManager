using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MH.UI.WPF.Controls;

public class CalendarExt : Calendar {
  protected override void OnPreviewMouseUp(MouseButtonEventArgs e) {
    base.OnPreviewMouseUp(e);

    if (Mouse.Captured is Calendar || Mouse.Captured is CalendarItem)
      Mouse.Capture(null);
  }
}