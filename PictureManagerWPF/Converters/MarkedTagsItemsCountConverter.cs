using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace PictureManager.Converters {
  public class MarkedTagsItemsCountConverter : IMultiValueConverter {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
      values?.Length == 2 && values[0] is Dictionary<object, int> tags
        ? tags.ContainsKey(values[1])
          ? tags[values[1]].ToString()
          : string.Empty
        : string.Empty;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
