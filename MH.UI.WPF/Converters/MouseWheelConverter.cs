using System.Windows;
using System.Windows.Input;

namespace MH.UI.WPF.Converters;

public class MouseWheelConverter : BaseMarkupExtensionConverter {
  public override object Convert(object value, object parameter) {
    if (value is not MouseWheelEventArgs e) return null;

    return new MH.Utils.EventsArgs.MouseWheelEventArgs() {
      IsSourceDesired = parameter?.Equals(e.OriginalSource.GetType().FullName) == true,
      OriginalSource = e.OriginalSource,
      DataContext = (e.OriginalSource as FrameworkElement)?.DataContext,
      Delta = e.Delta,
      IsCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0,
      IsAltOn = (Keyboard.Modifiers & ModifierKeys.Alt) > 0,
      IsShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0
    };
  }
}