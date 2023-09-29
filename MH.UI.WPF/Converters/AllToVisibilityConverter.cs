using System.Windows;

namespace MH.UI.WPF.Converters;

public class AllToVisibilityConverter : BaseMarkupExtensionConverter {
  public override object Convert(object value, object parameter) =>
    AllToBoolConverter.AllToBool(value, parameter)
      ? Visibility.Visible
      : Visibility.Collapsed;
}