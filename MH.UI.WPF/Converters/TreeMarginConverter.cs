using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace MH.UI.WPF.Converters {
  public class TreeMarginConverter : MarkupExtension, IValueConverter {
    private static readonly TreeMarginConverter _converter = null;
    private const string _parentPropName = "Parent";

    public override object ProvideValue(IServiceProvider serviceProvider) =>
      _converter ?? new TreeMarginConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      var length = int.TryParse(parameter as string, out var l) ? l : 0;
      var level = 0;
      var parent = GetParent(value);

      while (parent != null) {
        level++;
        parent = GetParent(parent);
      }

      return new Thickness(length * level, 0.0, 0.0, 0.0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();

    private static object GetParent(object o) =>
      o?.GetType().GetProperty(_parentPropName)?.GetValue(o, null);
  }
}
