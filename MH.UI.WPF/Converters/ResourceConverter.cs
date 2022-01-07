using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MH.UI.WPF.Converters {
  public class ResourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value == null
        ? DependencyProperty.UnsetValue
        : Application.Current.TryFindResource(value)
          ?? DependencyProperty.UnsetValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
