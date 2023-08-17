using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace MH.UI.WPF.Converters {
  public class TypeToIconColorConverter : MarkupExtension, IValueConverter {
    private static readonly TypeToIconColorConverter _converter = null;
    public static Func<object, object, string> TypeToIconColor { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
      _converter ?? new TypeToIconColorConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      TypeToIconColor == null
        ? DependencyProperty.UnsetValue
        : ResourceConverter.Convert(TypeToIconColor(value, parameter), parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}