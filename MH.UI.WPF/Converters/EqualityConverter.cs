using System;
using System.Globalization;
using System.Windows.Data;

namespace MH.UI.WPF.Converters {
  public class EqualityConverter : IMultiValueConverter {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
      if (values?.Length != 3 || values[0] is not int valA || values[1] is not string op || values[2] is not int valB)
        throw new ArgumentException(null, nameof(values));

      return op.ToLower() switch {
        "eq" => valA == valB,
        "ne" => valA != valB,
        "gt" => valA > valB,
        "ge" => valA >= valB,
        "lt" => valA < valB,
        "le" => valA <= valB,
        _ => throw new NotSupportedException(op)
      };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
