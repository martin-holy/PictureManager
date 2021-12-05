using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MH.UI.WPF.Converters {
  public class ClickConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value is not MouseButtonEventArgs args ? null : new ClickEventArgs(args, parameter is true);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }

  public class ClickEventArgs {
    public int ClickCount { get; }
    public object Source { get; }
    public object DataContext { get; set; }
    public bool IsCtrlOn { get; }
    public bool IsAltOn { get; }
    public bool IsShiftOn { get; }

    public ClickEventArgs(MouseButtonEventArgs args, bool allButtons) {
      ClickCount = args.ClickCount;
      Source = args.Source;
      DataContext = (args.Source as FrameworkElement)?.DataContext;
      IsCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      IsAltOn = (Keyboard.Modifiers & ModifierKeys.Alt) > 0;
      IsShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;

      // right button like CTRL + left button
      if (allButtons && args.ChangedButton is MouseButton.Right) {
        IsCtrlOn = true;
        IsShiftOn = false;
      }
    }
  }
}
