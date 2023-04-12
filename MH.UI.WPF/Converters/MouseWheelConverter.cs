using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MH.UI.WPF.Converters {
  public class MouseWheelConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      GetArgs(value, parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();

    private MH.Utils.EventsArgs.MouseWheelEventArgs GetArgs(object value, object desiredSource) {
      if (value is not MouseWheelEventArgs e) return null;

      return new MH.Utils.EventsArgs.MouseWheelEventArgs() {
        IsSourceDesired = desiredSource == null || e.OriginalSource.GetType().FullName.Equals(desiredSource),
        OriginalSource = e.OriginalSource,
        DataContext = (e.OriginalSource as FrameworkElement)?.DataContext,
        Delta = e.Delta,
        IsCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0,
        IsAltOn = (Keyboard.Modifiers & ModifierKeys.Alt) > 0,
        IsShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0
      };
    }
  }
}
