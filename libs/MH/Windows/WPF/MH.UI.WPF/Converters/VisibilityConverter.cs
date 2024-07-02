using System.Collections;
using System.Windows;
using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public enum CheckFor { NotNull, Null, True, False, NotEmpty, NullOrEmpty, All }

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
  private static VisibilityConverter _trueToVisible;
  private static VisibilityConverter _falseToVisible;
  private static VisibilityConverter _trueToHidden;
  private static VisibilityConverter _falseToHidden;
  private static VisibilityConverter _notEmptyToVisible;
  private static VisibilityConverter _nullOrEmptyToVisible;

  public static VisibilityConverter AllToCollapsed => _allToCollapsed ??= new() { CheckFor = CheckFor.All, ToCollapsed = true };
  public static VisibilityConverter AllToHidden => _allToHidden ??= new() { CheckFor = CheckFor.All, ToHidden = true };
  public static VisibilityConverter AllToVisible => _allToVisible ??= new() { CheckFor = CheckFor.All, ToVisible = true };
  public static VisibilityConverter NullToCollapsed => _nullToCollapsed ??= new() { CheckFor = CheckFor.Null, ToCollapsed = true };
  public static VisibilityConverter NullToHidden => _nullToHidden ??= new() { CheckFor = CheckFor.Null, ToHidden = true };
  public static VisibilityConverter NullToVisible => _nullToVisible ??= new() { CheckFor = CheckFor.Null, ToVisible = true };
  public static VisibilityConverter NotNullToCollapsed => _notNullToCollapsed ??= new() { CheckFor = CheckFor.NotNull, ToCollapsed = true };
  public static VisibilityConverter NotNullToHidden => _notNullToHidden ??= new() { CheckFor = CheckFor.NotNull, ToHidden = true };
  public static VisibilityConverter NotNullToVisible => _notNullToVisible ??= new() { CheckFor = CheckFor.NotNull, ToVisible = true };
  public static VisibilityConverter TrueToVisible => _trueToVisible ??= new() { CheckFor = CheckFor.True, ToVisible = true };
  public static VisibilityConverter FalseToVisible => _falseToVisible ??= new() { CheckFor = CheckFor.False, ToVisible = true };
  public static VisibilityConverter TrueToHidden => _trueToHidden ??= new() { CheckFor = CheckFor.True, ToHidden = true };
  public static VisibilityConverter FalseToHidden => _falseToHidden ??= new() { CheckFor = CheckFor.False, ToHidden = true };
  public static VisibilityConverter NotEmptyToVisible => _notEmptyToVisible ??= new() { CheckFor = CheckFor.NotEmpty, ToVisible = true };
  public static VisibilityConverter NullOrEmptyToVisible => _nullOrEmptyToVisible ??= new() { CheckFor = CheckFor.NullOrEmpty, ToVisible = true };

  public CheckFor CheckFor { get; init; }

  public bool ToCollapsed { get; init; }
  public bool ToHidden { get; init; }
  public bool ToVisible { get; init; }

  public override object Convert(object value, object parameter) =>
    CheckFor switch {
      CheckFor.NotNull => GetFor(value != null),
      CheckFor.Null => GetFor(value == null),
      CheckFor.True => GetFor(value is true),
      CheckFor.False => GetFor(value is false),
      CheckFor.NotEmpty => GetFor(value is IList { Count: > 0 }),
      CheckFor.NullOrEmpty => GetFor(value is null or IList { Count: 0 }),
      CheckFor.All => GetFor(AllToBoolConverter.AllToBool(value, parameter)),
      _ => Binding.DoNothing
    };

  private Visibility GetFor(bool flag) {
    if (flag) {
      if (ToVisible) return Visibility.Visible;
      if (ToCollapsed) return Visibility.Collapsed;
      if (ToHidden) return Visibility.Hidden;
    }
    else {
      if (ToVisible) return Visibility.Collapsed;
      if (ToCollapsed || ToHidden) return Visibility.Visible;
    }

    return Visibility.Collapsed;
  }
}