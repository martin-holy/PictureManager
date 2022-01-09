﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MH.UI.WPF.Converters {
  public class ResourceConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      if (value == null)
        return DependencyProperty.UnsetValue;

      if (parameter is Dictionary<object, object> dict)
        if (!dict.TryGetValue(value, out value) || value == null)
          return DependencyProperty.UnsetValue;

      return Application.Current.TryFindResource(value)
             ?? DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
