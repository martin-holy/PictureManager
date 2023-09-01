using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace MH.UI.WPF.Converters {
  public class DebugConverter : MarkupExtension, IValueConverter {
    private static readonly DebugConverter _converter = null;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
      _converter ?? new DebugConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
