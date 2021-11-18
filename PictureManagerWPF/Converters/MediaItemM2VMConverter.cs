using System;
using System.Globalization;
using System.Windows.Data;
using PictureManager.Domain.Models;

namespace PictureManager.Converters {
  public class MediaItemM2VMConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null) throw new ArgumentNullException(nameof(value));

      return App.Ui.MediaItemsBaseVM.ToViewModel(value as MediaItemM);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
