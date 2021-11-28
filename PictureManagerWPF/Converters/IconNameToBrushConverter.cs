using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PictureManager.Converters {
  public class IconNameToBrushConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException(nameof(value));

      var resourceName = value switch {
        "IconFolder" or
        "IconFolderStar" or
        "IconFolderLock" or
        "IconFolderPuzzle" or
        "IconFolderOpen" => "ColorBrushFolder",

        "IconTag" or
        "IconTagLabel" => "ColorBrushTag",

        "IconPeople" or
        "IconPeopleMultiple" => "ColorBrushPeople",

        "IconDrive" or
        "IconDriveError" or
        "IconCd" => "ColorBrushDrive",

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
