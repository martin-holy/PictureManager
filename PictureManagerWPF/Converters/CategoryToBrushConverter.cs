using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using PictureManager.Domain;

namespace PictureManager.Converters {
  public class CategoryToBrushConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException(nameof(value));

      var resourceName = value switch {
        Category.Folders or
        Category.FavoriteFolders or
        Category.FolderKeywords => "ColorBrushFolder",
        Category.People => "ColorBrushPeople",
        Category.Keywords => "ColorBrushTag",
        Category.Ratings or
        Category.Viewers or
        Category.MediaItemClips or
        _ => "ColorBrushWhite"
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
