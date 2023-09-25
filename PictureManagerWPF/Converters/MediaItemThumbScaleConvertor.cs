using MH.UI.WPF.Converters;
using PictureManager.Domain;
using PictureManager.Domain.DataViews;
using System;

namespace PictureManager.Converters;

public class MediaItemThumbScaleConvertor : BaseMarkupExtensionConverter {
  public override object Convert(object value, object parameter) =>
    Core.MediaItemsViews.Current?.ThumbScale is { } scale
    && Math.Abs(scale - MediaItemsViews.DefaultThumbScale) > 0 && value is int size
      ? Math.Round(size / MediaItemsViews.DefaultThumbScale * scale, 0)
      : value;
}