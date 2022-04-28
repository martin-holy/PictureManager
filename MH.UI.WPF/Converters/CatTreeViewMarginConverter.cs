using MH.Utils.Interfaces;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MH.UI.WPF.Converters {
  public class CatTreeViewMarginConverter : IValueConverter {
    public double Length { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value is not ITreeItem ti) return new Thickness(0.0);

      var levels = 1.5;
      var parent = ti.Parent;
      while (parent != null) {
        levels++;
        parent = parent.Parent;
      }

      var offset = parameter == null ? 0 : int.Parse((string)parameter);

      return new Thickness((Length * levels) + offset, 0.0, 0.0, 0.0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
  }
}
