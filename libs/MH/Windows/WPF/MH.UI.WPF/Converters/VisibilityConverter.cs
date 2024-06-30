using System.Windows;
using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public enum CheckFor { Null, NotNull, All }

public class VisibilityConverter : BaseConverter {
  private static VisibilityConverter _allToCollapsed;
  private static VisibilityConverter _allToHidden;
  private static VisibilityConverter _allToVisible;
  private static VisibilityConverter _nullToCollapsed;
  private static VisibilityConverter _nullToHidden;
  private static VisibilityConverter _nullToVisible;
  private static VisibilityConverter _notNullToCollapsed;
  private static VisibilityConverter _notNullToHidden;
  private static VisibilityConverter _notNullToVisible;

  public static VisibilityConverter AllToCollapsed => _allToCollapsed ??= new() { CheckFor = CheckFor.All, ToCollapsed = true };
  public static VisibilityConverter AllToHidden => _allToHidden ??= new() { CheckFor = CheckFor.All, ToHidden = true };
  public static VisibilityConverter AllToVisible => _allToVisible ??= new() { CheckFor = CheckFor.All, ToVisible = true };
  public static VisibilityConverter NullToCollapsed => _nullToCollapsed ??= new() { CheckFor = CheckFor.Null, ToCollapsed = true };
  public static VisibilityConverter NullToHidden => _nullToHidden ??= new() { CheckFor = CheckFor.Null, ToHidden = true };
  public static VisibilityConverter NullToVisible => _nullToVisible ??= new() { CheckFor = CheckFor.Null, ToVisible = true };
  public static VisibilityConverter NotNullToCollapsed => _notNullToCollapsed ??= new() { CheckFor = CheckFor.NotNull, ToCollapsed = true };
  public static VisibilityConverter NotNullToHidden => _notNullToHidden ??= new() { CheckFor = CheckFor.NotNull, ToHidden = true };
  public static VisibilityConverter NotNullToVisible => _notNullToVisible ??= new() { CheckFor = CheckFor.NotNull, ToVisible = true };

  public CheckFor CheckFor { get; init; }

  public bool ToCollapsed { get; init; }
  public bool ToHidden { get; init; }
  public bool ToVisible { get; init; }

  public override object Convert(object value, object parameter) =>
    CheckFor switch {
      CheckFor.Null => GetFor(value == null),
      CheckFor.NotNull => GetFor(value != null),
      CheckFor.All => GetFor(AllToBoolConverter.AllToBool(value, parameter)),
      _ => Binding.DoNothing
    };

  private Visibility GetFor(bool flag) {
    if (flag) {
      if (ToCollapsed) return Visibility.Collapsed;
      if (ToHidden) return Visibility.Hidden;
      if (ToVisible) return Visibility.Visible;
    }
    else {
      if (ToCollapsed || ToHidden) return Visibility.Visible;
      if (ToVisible) return Visibility.Collapsed;
    }

    return Visibility.Collapsed;
  }
}