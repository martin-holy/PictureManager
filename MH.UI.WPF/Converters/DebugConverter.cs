using System;
using System.Diagnostics;
using System.Globalization;

namespace MH.UI.WPF.Converters; 

public class DebugConverter : BaseMarkupExtensionConverter {
  public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
    Debug.WriteLine($"DebugConverter value: {value}, parameter: {parameter}");
    return value;
  }
}