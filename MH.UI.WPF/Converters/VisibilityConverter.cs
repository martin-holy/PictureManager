using System.Windows;
using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public class VisibilityConverter : BaseConverter {
  private static readonly object _lock = new();
  private static VisibilityConverter _allToCollapsed;
  private static VisibilityConverter _allToVisible;

  public static VisibilityConverter AllToCollapsed { get {
    lock (_lock) {
      return _allToCollapsed ??= new() { ToCollapsed = true };
    } } }

  public static VisibilityConverter AllToVisible { get {
    lock (_lock) {
      return _allToVisible ??= new() { ToVisible = true };
    } } }

  public bool ToCollapsed { get; init; }
  public bool ToVisible { get; init; }

  public override object Convert(object value, object parameter) {
    if (AllToBoolConverter.AllToBool(value, parameter)) {
      if (ToCollapsed) return Visibility.Collapsed;
      if (ToVisible) return Visibility.Visible;
    }
    else {
      if (ToCollapsed) return Visibility.Visible;
      if (ToVisible) return Visibility.Collapsed;
    }

    return Binding.DoNothing;
  }
}