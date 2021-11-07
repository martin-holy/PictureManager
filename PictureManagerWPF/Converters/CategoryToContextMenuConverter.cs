using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PictureManager.Domain;

namespace PictureManager.Converters {
  public class CategoryToContextMenuConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException(nameof(value));

      var resourceName = value switch {
        Category.People => "CatPeopleContextMenu",
        Category.FolderKeywords => "CatFolderKeywordsContextMenu",
        Category.Keywords => "CatKeywordsContextMenu",
        Category.GeoNames => "CatGeoNamesContextMenu",
        Category.Viewers => "CatViewersContextMenu",
        Category.VideoClips => "CatVideoClipsContextMenu",
        _ => string.Empty
      };

      if (string.Empty.Equals(resourceName))
        return null;

      try {
        return Application.Current.FindResource(resourceName);
      }
      catch (Exception ex) {
        App.Core.LogError(ex);
        return null;
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
