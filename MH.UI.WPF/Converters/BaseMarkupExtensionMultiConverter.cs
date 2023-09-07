using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace MH.UI.WPF.Converters {
  public class BaseMarkupExtensionMultiConverter : MarkupExtension, IMultiValueConverter {
    private static readonly IMultiValueConverter _converter = null;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
      _converter ?? Activator.CreateInstance(GetType());

    public virtual object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
      Convert(values, parameter);

    public virtual object Convert(object[] values, object parameter) =>
      throw new NotImplementedException();

    public virtual object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
