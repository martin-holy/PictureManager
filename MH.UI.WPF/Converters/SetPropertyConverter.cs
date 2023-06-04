using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace MH.UI.WPF.Converters {
  public class SetPropertyConverter : MarkupExtension, IMultiValueConverter {
    private static readonly SetPropertyConverter _converter = null;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
      _converter ?? new SetPropertyConverter();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
      if (values.Length == 2 && parameter is string propName)
        values[0]?.GetType().GetProperty(propName)?.SetValue(values[0], values[1]);

      return Binding.DoNothing;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}