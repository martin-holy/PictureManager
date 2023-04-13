using PictureManager.Domain;
using PictureManager.Domain.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PictureManager.Converters {
  public class MediaViewerImageSourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      try {
        if (value is not MediaItemM mi || mi.MediaType != Domain.MediaType.Image) return null;

        return Utils.Imaging.GetBitmapImage(mi.FilePath, (MediaOrientation)mi.Orientation);
      }
      catch (Exception ex) {
        Console.WriteLine(ex);
        return null;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
  }
}
