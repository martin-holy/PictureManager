using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MH.UI.WPF.Converters {
  public class MouseButtonConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      GetArgs(value, parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();

    private MH.Utils.EventsArgs.MouseButtonEventArgs GetArgs(object value, object desiredSource) {
      if (value is not MouseButtonEventArgs e) return null;

      var args = new MH.Utils.EventsArgs.MouseButtonEventArgs() {
        IsSourceDesired = desiredSource == null || e.OriginalSource.GetType().FullName.Equals(desiredSource),
        OriginalSource = e.OriginalSource,
        DataContext = (e.OriginalSource as FrameworkElement)?.DataContext,
        ClickCount = e.ClickCount,
        IsAltOn = (Keyboard.Modifiers & ModifierKeys.Alt) > 0
      };

      if (e.ChangedButton is not MouseButton.Left) {
        args.IsCtrlOn = true;
        args.IsShiftOn = false;
      }
      else {
        args.IsCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
        args.IsShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      }

      return args;
    }
  }
}
