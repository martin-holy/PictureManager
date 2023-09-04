using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace MH.UI.WPF.Converters {
  public class BaseMarkupExtensionConverter : MarkupExtension, IValueConverter {
    private static readonly IValueConverter _converter = null;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
      _converter ?? Activator.CreateInstance(GetType());

    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      Convert(value, parameter);

    public virtual object Convert(object value, object parameter) =>
      throw new NotImplementedException();

    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
