using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PictureManager {
  public class RatingConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException(nameof(value));
      if (parameter == null) throw new ArgumentNullException(nameof(parameter));

      return int.Parse((string)parameter) < (int)value
        ? new SolidColorBrush(Color.FromRgb(255, 255, 255))
        : new SolidColorBrush(Color.FromRgb(104, 104, 104));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }

  public class MediaItemSizeConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value == null ? string.Empty : $"{Math.Round((double)value / 1000000, 1)} MPx";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
  }  
}
