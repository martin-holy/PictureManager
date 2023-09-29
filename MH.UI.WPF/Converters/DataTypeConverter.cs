using System;

namespace MH.UI.WPF.Converters; 

public class DataTypeConverter : BaseMarkupExtensionConverter {
  public override object Convert(object value, object parameter) =>
    value != null && parameter is Type pType && (pType.IsInterface
      ? value.GetType().GetInterface(pType.Name) != null
      : value.GetType().IsAssignableTo(pType));
}