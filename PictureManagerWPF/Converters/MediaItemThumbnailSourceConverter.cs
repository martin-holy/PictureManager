using MH.Utils;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

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
            Core.Settings.ThumbnailSize,
            0,
            Core.Settings.JpegQualityLevel).GetAwaiter().GetResult();

        var orientation = mi.Orientation;
        // swap 90 and 270 degrees for video
        if (mi.MediaType == MediaType.Video) {
          if (mi.Orientation == 6)
            orientation = 8;
          else if (mi.Orientation == 8)
            orientation = 6;
        }

        var src = new BitmapImage();
        src.BeginInit();
        src.CacheOption = BitmapCacheOption.OnLoad;
        src.UriSource = new(mi.FilePathCache);
        src.Rotation = Imaging.MediaOrientation2Rotation((MediaOrientation)orientation);

        if (MediaItemsM.ThumbIgnoreCache.Remove(mi))
          src.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

        src.EndInit();

        return src;
      }
      catch (Exception ex) {
        Log.Error(ex);
        return Binding.DoNothing;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
