using System;
using System.Globalization;
using System.IO;
using System.Net.Cache;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PictureManager.Converters {
  public class ImageSourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      try {
        if (value is not string filePath || !File.Exists(filePath)) return null;

        var src = new BitmapImage();
        src.BeginInit();

        if ("IgnoreImageCache".Equals(parameter as string, StringComparison.Ordinal)) {
          src.UriCachePolicy = new(RequestCacheLevel.BypassCache);
          src.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        }

        src.CacheOption = BitmapCacheOption.OnLoad;
        src.UriSource = new(filePath);
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
}
