using System.Collections;
using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public class ToBoolConverter(CheckFor checkFor) : BaseConverter {
  private static ToBoolConverter? _isNull;
  private static ToBoolConverter? _isNotNull;
  private static ToBoolConverter? _isNotEmpty;
  private static ToBoolConverter? _isNullOrEmpty;
  private static ToBoolConverter? _isMoreThan0;

  public static ToBoolConverter IsNull => _isNull ??= new(CheckFor.Null);
  public static ToBoolConverter IsNotNull => _isNotNull ??= new(CheckFor.NotNull);
  public static ToBoolConverter IsNotEmpty => _isNotEmpty ??= new(CheckFor.NotEmpty);
  public static ToBoolConverter IsNullOrEmpty => _isNullOrEmpty ??= new(CheckFor.NullOrEmpty);
  public static ToBoolConverter IsMoreThan0 => _isMoreThan0 ??= new(CheckFor.MoreThan0);

  public CheckFor CheckFor { get; init; } = checkFor;

  public override object Convert(object? value, object? parameter) =>
    CheckFor switch {
      CheckFor.NotNull => value != null,
      CheckFor.Null => value == null,
      CheckFor.NotEmpty => value is IList { Count: > 0 },
      CheckFor.NullOrEmpty => value is null or IList { Count: 0 },
      CheckFor.MoreThan0 => value is > 0,
      _ => Binding.DoNothing
    };
}