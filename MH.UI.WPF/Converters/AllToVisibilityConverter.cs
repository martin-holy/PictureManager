using System.Windows;

namespace MH.UI.WPF.Converters;

public class AllToVisibilityConverter : BaseConverter {
  private static readonly object _lock = new();
  private static AllToVisibilityConverter _inst;
  public static AllToVisibilityConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object value, object parameter) =>
    AllToBoolConverter.AllToBool(value, parameter)
      ? Visibility.Visible
      : Visibility.Collapsed;
}