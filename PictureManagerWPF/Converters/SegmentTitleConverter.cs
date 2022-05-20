using PictureManager.Domain.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace PictureManager.Converters {
  public class SegmentTitleConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
      value == null
        ? Binding.DoNothing
        : SegmentsM.GetSegmentTitle((SegmentM)value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotImplementedException();
  }
}
