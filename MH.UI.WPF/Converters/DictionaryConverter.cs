using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MH.UI.WPF.Converters {
  public class DictionaryConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value == null
      || parameter is not Dictionary<object, object> dict
      || !dict.TryGetValue(value, out var resName)
      || resName == null
        ? DependencyProperty.UnsetValue
        : Application.Current.TryFindResource(resName)
          ?? DependencyProperty.UnsetValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
