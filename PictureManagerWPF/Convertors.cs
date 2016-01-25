using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PictureManager {
  public class StaticResourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      return Application.Current.FindResource((string)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new Exception("The method or operation is not implemented.");
    }
  }
}
