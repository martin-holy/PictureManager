using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public class SetPropertyConverter : BaseMarkupExtensionMultiConverter {
  public override object Convert(object[] values, object parameter) {
    if (values.Length == 2 && parameter is string propName)
      values[0]?.GetType().GetProperty(propName)?.SetValue(values[0], values[1]);

    return Binding.DoNothing;
  }
}