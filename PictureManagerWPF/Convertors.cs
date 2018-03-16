using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PictureManager {
  public class StaticResourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException();
      return Application.Current.FindResource((string) value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new NotSupportedException();
    }
  }

  public class IconNameToStaticResourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException();

      string resourceName;
      switch ((IconName) value) {
        case IconName.Folder: resourceName = "appbar_folder"; break;
        case IconName.FolderStar: resourceName = "appbar_folder_star"; break;
        case IconName.FolderLock: resourceName = "appbar_folder_lock"; break;
        case IconName.FolderOpen: resourceName = "appbar_folder_open"; break;
        case IconName.Star: resourceName = "appbar_star"; break;
        case IconName.People: resourceName = "appbar_people"; break;
        case IconName.PeopleMultiple: resourceName = "appbar_people_multiple"; break;
        case IconName.Tag: resourceName = "appbar_tag"; break;
        case IconName.TagLabel: resourceName = "appbar_tag_label"; break;
        case IconName.Filter: resourceName = "appbar_filter"; break;
        case IconName.Eye: resourceName = "appbar_eye"; break;
        case IconName.DatabaseSql: resourceName = "appbar_database_sql"; break;
        case IconName.Bug: resourceName = "appbar_bug"; break;
        case IconName.LocationCheckin: resourceName = "appbar_location_checkin"; break;
        case IconName.Notification: resourceName = "appbar_notification"; break;
        case IconName.Cd: resourceName = "appbar_cd"; break;
        case IconName.Drive: resourceName = "appbar_drive"; break;
        case IconName.DriveError: resourceName = "appbar_drive_error"; break;
        case IconName.Cancel: resourceName = "appbar_cancel"; break;
        case IconName.Save: resourceName = "appbar_save"; break;
        default: resourceName = "appbar_bug"; break;
      }
      return Application.Current.FindResource(resourceName);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new NotSupportedException();
    }
  }

  public class IconNameToBrush : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException();

      switch ((IconName) value) {
        case IconName.Folder:
        case IconName.FolderStar:
        case IconName.FolderLock:
        case IconName.FolderOpen: return new SolidColorBrush(Color.FromRgb(249, 218, 119));
        case IconName.Tag: return new SolidColorBrush(Color.FromRgb(142, 193, 99));
        case IconName.TagLabel: return new SolidColorBrush(Color.FromRgb(142, 193, 99));
        case IconName.People:
        case IconName.PeopleMultiple: return new SolidColorBrush(Color.FromRgb(19, 122, 166));
        case IconName.Drive:
        case IconName.DriveError:
        case IconName.Cd: return new SolidColorBrush(Color.FromRgb(199, 199, 199));
        case IconName.Cancel: return new SolidColorBrush(Color.FromRgb(221, 121, 47));
        case IconName.Save: return new SolidColorBrush(Color.FromRgb(19, 122, 166));
        default: return new SolidColorBrush(Color.FromRgb(255, 255, 255));
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new NotSupportedException();
    }
  }

  public class RatingConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null || parameter == null) throw new ArgumentNullException();
      return int.Parse((string) parameter) < (int) value
        ? new SolidColorBrush(Color.FromRgb(255, 255, 255))
        : new SolidColorBrush(Color.FromRgb(104, 104, 104));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new NotSupportedException();
    }
  }

  public class BackgroundColorConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException();

      switch ((BackgroundBrush)value) {
        case BackgroundBrush.AndThis: return new SolidColorBrush(Color.FromRgb(142, 193, 99));
        case BackgroundBrush.OrThis: return new SolidColorBrush(Color.FromRgb(21, 133, 181));
        case BackgroundBrush.Hidden: return new SolidColorBrush(Color.FromRgb(222, 87, 58));
        default: return new SolidColorBrush(Color.FromRgb(37, 37, 37));
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new NotSupportedException();
    }
  }

  public class DataBindingDebugConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      return value;
    }
  }

  public class ImageSourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      var src = new BitmapImage();
      src.BeginInit();
      src.UriSource = (Uri) value;
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.EndInit();
      return src;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      return value;
    }
  }
}
