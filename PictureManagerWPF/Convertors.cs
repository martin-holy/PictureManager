using System;
using System.Globalization;
using System.Runtime.Remoting.Channels;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PictureManager {
  public class StaticResourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      return Application.Current.FindResource((string)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new Exception("The method or operation is not implemented.");
    }
  }

  public class IconNameToBrush : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) value = parameter;
      switch ((string) value) {
        case "appbar_folder":
        case "appbar_folder_star":
        case "appbar_folder_lock":
        case "appbar_folder_open": return new SolidColorBrush(Color.FromRgb(249, 218, 119));
        case "appbar_tag": return new SolidColorBrush(Color.FromRgb(142, 193, 99));
        case "appbar_people_multiple":
        case "appbar_people": return new SolidColorBrush(Color.FromRgb(19, 122, 166));
        case "appbar_drive":
        case "appbar_drive_error":
        case "appbar_cd": return new SolidColorBrush(Color.FromRgb(199, 199, 199));
        case "appbar_cancel": return new SolidColorBrush(Color.FromRgb(221, 121, 47));
        case "appbar_save": return new SolidColorBrush(Color.FromRgb(19, 122, 166));
        default: return new SolidColorBrush(Color.FromRgb(255, 255, 255));
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new Exception("The method or operation is not implemented.");
    }
  }
}
