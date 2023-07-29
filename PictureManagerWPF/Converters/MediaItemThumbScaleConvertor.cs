using PictureManager.Domain.DataViews;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PictureManager.Converters {
  public class MediaItemThumbScaleConvertor : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      App.Core.MediaItemsViews.Current?.ThumbScale is { } scale
        && Math.Abs(scale - MediaItemsViews.DefaultThumbScale) > 0 && value is int size
        ? Math.Round(size / MediaItemsViews.DefaultThumbScale * scale, 0)
        : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
