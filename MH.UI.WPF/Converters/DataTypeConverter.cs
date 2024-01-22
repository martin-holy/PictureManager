using System;

namespace MH.UI.WPF.Converters; 

public class DataTypeConverter : BaseConverter {
  private static readonly object _lock = new();
  private static DataTypeConverter _inst;
  public static DataTypeConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object value, object parameter) =>
    value != null && parameter is Type pType && (pType.IsInterface
      ? value.GetType().GetInterface(pType.Name) != null
      : value.GetType().IsAssignableTo(pType));
}