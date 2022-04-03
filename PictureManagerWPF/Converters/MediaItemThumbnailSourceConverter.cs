using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PictureManager.Domain.Utils;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using System.Windows;

namespace PictureManager.Converters {
  public class MediaItemThumbnailSourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      try {
        if (value is not MediaItemM mi)
          return Binding.DoNothing;

        if (!File.Exists(mi.FilePathCache))
          Imaging.CreateThumbnailAsync(
            mi.MediaType,
            mi.FilePath,
            mi.FilePathCache,
            Settings.Default.ThumbnailSize,
            0,
            Settings.Default.JpegQualityLevel).GetAwaiter().GetResult();

        var src = new BitmapImage();
        src.BeginInit();
        src.CacheOption = BitmapCacheOption.OnLoad;
        src.UriSource = new(mi.FilePathCache);
        src.EndInit();

        return src;
      }
      catch (Exception ex) {
        App.Core.LogError(ex);
        return Binding.DoNothing;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
