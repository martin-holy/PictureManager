using System;
using System.Diagnostics;
using System.Globalization;

namespace MH.UI.WPF.Converters; 

public class DebugConverter : BaseConverter {
  private static readonly object _lock = new();
  private static DebugConverter _inst;
  public static DebugConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
    Debug.WriteLine($"DebugConverter value: {value}, parameter: {parameter}");
    return value;
  }
}