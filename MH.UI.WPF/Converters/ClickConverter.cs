using MH.Utils.EventsArgs;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MH.UI.WPF.Converters {
  public class ClickConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value is not MouseButtonEventArgs args
        ? null
        : GetArgs(args, parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();

    private ClickEventArgs GetArgs(MouseButtonEventArgs e, object desiredSource) {
      var args = new ClickEventArgs() {
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
