using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PictureManager.Domain.Models;

namespace PictureManager.Converters {
  public class SegmentThumbnailSourceConverter : IMultiValueConverter {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
      try {
        if (values?.Length != 2 || values[1] is not SegmentM segment) return null;

        if (!File.Exists(segment.FilePathCache))
          segment.CreateThumbnail();

        var src = new BitmapImage();
        src.BeginInit();
        src.CacheOption = BitmapCacheOption.OnLoad;
        src.UriSource = new(segment.FilePathCache);

        if (segment.Equals(App.Core.SegmentsM.IgnoreImageCacheSegment)) {
          App.Core.SegmentsM.IgnoreImageCacheSegment = null;
          src.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        }

        src.EndInit();

        return src;
      }
      catch (Exception ex) {
        Debug.WriteLine(ex);
        return null;
      }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
