using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace MH.UI.WPF.Converters {
  public class AllToBoolConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      AllToBool(value, parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();

    public static bool AllToBool(object value, object parameter) {
      if (value == null) return false;

      if (parameter != null)
        return value switch {
          string s => s.Equals(parameter),
          int i => i.Equals(int.Parse((string)parameter)),
          _ => value.Equals(parameter),
        };

      return value switch {
        string s => !string.IsNullOrEmpty(s),
        bool b => b,
        int i => i > 0,
        Collection<string> c => c.Count > 0,
        _ => true, // value != null
      };
    }
  }
}
