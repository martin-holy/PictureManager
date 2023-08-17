using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace MH.UI.WPF.Converters {
  public class TypeToIconConverter : MarkupExtension, IValueConverter {
    private static readonly TypeToIconConverter _converter = null;
    public static Func<object, object, string> TypeToIcon { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
      _converter ?? new TypeToIconConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      TypeToIcon == null
        ? DependencyProperty.UnsetValue
        : ResourceConverter.Convert(TypeToIcon(value, parameter), parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
