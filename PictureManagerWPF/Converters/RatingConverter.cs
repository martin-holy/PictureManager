using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace PictureManager.Converters {
  public class RatingConverter : MarkupExtension, IValueConverter {
    private static RatingConverter _converter = null;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
      _converter ?? new RatingConverter();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value is not int v || !int.TryParse(parameter as string, out int p))
        return Binding.DoNothing;

      return p < v
        ? new SolidColorBrush(Color.FromRgb(255, 255, 255))
        : new SolidColorBrush(Color.FromRgb(104, 104, 104));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
