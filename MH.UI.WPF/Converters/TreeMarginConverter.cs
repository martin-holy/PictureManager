using MH.Utils;
using MH.Utils.Interfaces;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace MH.UI.WPF.Converters {
  public class TreeMarginConverter : MarkupExtension, IValueConverter {
    private static readonly TreeMarginConverter _converter = null;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
      _converter ?? new TreeMarginConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      var level = value is ITreeItem ti ? ti.GetLevel() : 0;
      var length = int.TryParse(parameter as string, out var l) ? l : 0;

      return new Thickness(length * level, 0.0, 0.0, 0.0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
