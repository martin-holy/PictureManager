using System;
using System.Globalization;
using System.Windows.Data;

namespace MH.UI.WPF.Converters {
  public class DataTypeConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value != null && parameter is Type pType && (pType.IsInterface
        ? value.GetType().GetInterface(pType.Name) != null
        : value.GetType().IsAssignableTo(pType));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
