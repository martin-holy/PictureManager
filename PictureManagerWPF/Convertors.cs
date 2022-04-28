using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PictureManager {
  public static class Convertors {
    public static bool AllToBool(object value, object parameter) {
      if (value == null) return false;

      if (parameter != null)
        return value switch {
          string s => s.Equals(parameter),
          int i => i.Equals(int.Parse((string)parameter)),
          _ => value.Equals(parameter),
        };

      return value switch {
        string s => !string.IsNullOrEmpty(s),
        bool b => b,
        int i => i > 0,
        Collection<string> c => c.Count > 0,
        _ => true, // value != null
      };
    }
  }

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

  public class AllToVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      Convertors.AllToBool(value, parameter) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }

  public class AllToBoolConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      Convertors.AllToBool(value, parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }

  public class MediaItemSizeConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value == null ? string.Empty : $"{Math.Round((double)value / 1000000, 1)} MPx";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
  }  
}
