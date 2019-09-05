using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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
      if (value == null)
        value = IconName.Bug;

      var resourceName = $"appbar{Regex.Replace(((IconName) value).ToString(), @"([A-Z])", "_$1").ToLower()}";
      
      return Application.Current.FindResource(resourceName);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new NotSupportedException();
    }
  }

  public class IconSymbolConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException();

      switch ((IconName) value) {
        case IconName.Folder: return "\U0001F4C1";
        case IconName.FolderOpen: return "\U0001F4C2";
        case IconName.Ruler: return "\U0001F4CF";
        default: return "";
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new NotSupportedException();
    }
  }

  public class TypeToStyleConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException();
      var dataContext = ((StackPanel) value).DataContext;

      switch (dataContext) {
        case Database.Keyword _:
        case Database.Folder _:
          return App.WMain.TcMain.FindResource("STreeViewStackPanelWithDragDrop");
        case Database.Person _:
          return App.WMain.TcMain.FindResource("STreeViewStackPanelWithDrag");
        case Database.CategoryGroup _:
        case Database.Keywords _:
        case Database.People _:
          return App.WMain.TcMain.FindResource("STreeViewStackPanelWithDrop");
        default:
          return App.WMain.TcMain.FindResource("STreeViewStackPanel");
      }
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

  public class AllToVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) return Visibility.Collapsed;
      var result = false;

      switch (value) {
        case string s: {
          result = !string.IsNullOrEmpty(s);
          break;
        }
        case bool b: {
          result = b;
          break;
        }
        case int i: {
          result = i > 0;
          break;
        }
      }

      return result ? Visibility.Visible : Visibility.Collapsed;
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
      try {
        var uriValue = value as Uri;
        if (uriValue == null) return null;

        if (!File.Exists(uriValue.LocalPath)) return null;

        var src = new BitmapImage();
        src.BeginInit();
        src.UriSource = uriValue;
        src.CacheOption = BitmapCacheOption.OnLoad;
        src.EndInit();
        return src;
      }
      catch (Exception ex) {
        Console.WriteLine(ex);
        return null;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      return value;
    }
  }

  public class MediaItemSizeConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      return value == null ? string.Empty : $"{Math.Round((double) value / 1000000, 1)} MPx";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      return value;
    }
  }
}
