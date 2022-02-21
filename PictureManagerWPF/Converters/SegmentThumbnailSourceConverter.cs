using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PictureManager.Domain.Models;

namespace PictureManager.Converters {
  public class SegmentThumbnailSourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      try {
        if (value is not SegmentM segment) return null;

        if (!File.Exists(segment.FilePathCache))
          segment.CreateThumbnail();

        var src = new BitmapImage();
        src.BeginInit();
        src.CacheOption = BitmapCacheOption.OnLoad;
        src.UriSource = new(segment.FilePathCache);
        src.EndInit();

        return src;
      }
      catch (Exception ex) {
        Debug.WriteLine(ex);
        return null;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      value;
  }
}
