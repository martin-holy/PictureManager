using System;
using System.Globalization;
using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public class BaseMultiConverter : IMultiValueConverter {
  public virtual object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
    Convert(values, parameter);

  public virtual object Convert(object[] values, object parameter) =>
    throw new NotImplementedException();

  public virtual object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}