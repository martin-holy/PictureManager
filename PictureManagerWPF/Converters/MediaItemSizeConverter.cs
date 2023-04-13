using System;
using System.Globalization;
using System.Windows.Data;

namespace PictureManager.Converters {
  public class MediaItemSizeConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value == null ? string.Empty : $"{Math.Round((double)value / 1000000, 1)} MPx";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
  }
}
