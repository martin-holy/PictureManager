using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PictureManager.Domain;

namespace PictureManager.Converters {
  public class DisplayFilterToBrushConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException(nameof(value));

      var resourceName = (DisplayFilter)value switch {
        DisplayFilter.And => "DisplayFilterAndBrush",
        DisplayFilter.Or => "DisplayFilterOrBrush",
        DisplayFilter.Not => "DisplayFilterNotBrush",
        _ => "TransparentBrush",
      };

      try {
        return Application.Current.FindResource(resourceName);
      }
      catch (Exception ex) {
        App.Core.LogError(ex);
        return null;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
