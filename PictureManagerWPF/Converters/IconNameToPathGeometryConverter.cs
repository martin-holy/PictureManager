using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PictureManager.Converters {
  public class IconNameToPathGeometryConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException(nameof(value));

      try {
        return Application.Current.FindResource(value);
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
