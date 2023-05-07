using System;
using System.Globalization;
using System.Windows.Data;

namespace MH.UI.WPF.Converters {
  public class EqualityConverter : IMultiValueConverter {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
      if (values?.Length != 3 || values[1] is not string op)
        return false;

      op = op.ToLower();

      if (values[0] is int iValA && values[2] is int iValB) {
        switch (op) {
          case "eq": return iValA == iValB;
          case "ne": return iValA != iValB;
          case "gt": return iValA > iValB;
          case "ge": return iValA >= iValB;
          case "lt": return iValA < iValB;
          case "le": return iValA <= iValB;
        }
      }

      if (values[0] is double dValA && values[2] is double dValB) {
        switch (op) {
          case "eq": return dValA == dValB;
          case "ne": return dValA != dValB;
          case "gt": return dValA > dValB;
          case "ge": return dValA >= dValB;
          case "lt": return dValA < dValB;
          case "le": return dValA <= dValB;
        }
      }

      return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
