using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PictureManager.Domain;

namespace PictureManager.Converters {
  public class CategoryToPathGeometryConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException(nameof(value));

      var resourceName = value switch {
        Category.FavoriteFolders => "IconFolderStar",
        Category.Folders => "IconFolder",
        Category.Ratings => "IconStar",
        Category.MediaItemSizes => "IconRuler",
        Category.People => "IconPeopleMultiple",
        Category.FolderKeywords => "IconFolder",
        Category.Keywords => "IconTagLabel",
        Category.GeoNames => "IconLocationCheckin",
        Category.Viewers => "IconEye",
        Category.MediaItemClips => "IconMovieClapper",
        _ => "IconBug"
      };

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