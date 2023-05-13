using PictureManager.Domain.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PictureManager.Converters {
  public class MediaItemThumbScaleConvertor : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      App.Core.ThumbnailsGridsM.Current?.ThumbScale is double scale
        && scale != ThumbnailsGridsM.DefaultThumbScale && value is int size
        ? Math.Round((size / ThumbnailsGridsM.DefaultThumbScale) * scale, 0)
        : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
