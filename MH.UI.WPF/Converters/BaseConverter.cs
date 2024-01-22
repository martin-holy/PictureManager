using System;
using System.Globalization;
using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public class BaseConverter : IValueConverter {
  public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
    Convert(value, parameter);

  public virtual object Convert(object value, object parameter) =>
    throw new NotImplementedException();

  public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}