using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net.Cache;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MH.UI.WPF.Interfaces;
using PictureManager.Domain;

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

  public class StaticResourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value == null ? throw new ArgumentNullException(nameof(value)) : Application.Current.FindResource((string)value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }

  public class IconNameToStaticResourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null)
        value = IconName.Bug;

      // transition to new icon resources
      if (targetType == typeof(PathGeometry) && Application.Current.TryFindResource($"Icon{(IconName)value}") is { } res)
        return res;

      var resourceName = $"appbar{Regex.Replace(((IconName)value).ToString(), @"([A-Z])", "_$1").ToLower(CultureInfo.CurrentCulture)}";

      return Application.Current.FindResource(resourceName);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }

  public class IconNameToBrushConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException(nameof(value));
      var resName = "ColorBrushWhite";

      switch ((IconName)value) {
        case IconName.Folder:
        case IconName.FolderStar:
        case IconName.FolderLock:
        case IconName.FolderPuzzle:
        case IconName.FolderOpen: resName = "ColorBrushFolder"; break;
        case IconName.Tag:
        case IconName.TagLabel: resName = "ColorBrushTag"; break;
        case IconName.People:
        case IconName.PeopleMultiple: resName = "ColorBrushPeople"; break;
        case IconName.Drive:
        case IconName.DriveError:
        case IconName.Cd: resName = "ColorBrushDrive"; break;
      }

      return App.WMain.FindResource(resName);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
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

  public class ImageSourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      try {
        var uriValue = value as Uri;
        if (uriValue == null) return null;

        if (!File.Exists(uriValue.LocalPath)) return null;

        var src = new BitmapImage();
        src.BeginInit();

        if (parameter is string s && s.Equals("IgnoreImageCache", StringComparison.Ordinal)) {
          src.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
          src.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        }

        src.CacheOption = BitmapCacheOption.OnLoad;
        src.UriSource = uriValue;
        src.EndInit();
        return src;
      }
      catch (Exception ex) {
        Console.WriteLine(ex);
        return null;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
  }

  public class MediaItemSizeConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value == null ? string.Empty : $"{Math.Round((double)value / 1000000, 1)} MPx";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
  }

  public class CatTreeViewMarginConverter : IValueConverter {
    public double Length { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value is not ICatTreeViewItem tvi) return new Thickness(0.0);

      var levels = 1.5;
      var parent = tvi.Parent;
      while (parent != null) {
        levels++;
        parent = parent.Parent;
      }

      var offset = parameter == null ? 0 : int.Parse((string)parameter);

      return new Thickness((Length * levels) + offset, 0.0, 0.0, 0.0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
  }

  public class DataTypeConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value?.GetType() ?? Binding.DoNothing;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
