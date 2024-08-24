using System.Collections;
using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public enum ValueIs { Null, Empty }

public class ToBoolConverter(ValueIs valueIs) : BaseConverter {
  private static ToBoolConverter? _isNull;
  private static ToBoolConverter? _isEmpty;

  public static ToBoolConverter IsNull => _isNull ??= new(ValueIs.Null);
  public static ToBoolConverter IsEmpty => _isEmpty ??= new(ValueIs.Empty);

  public ValueIs ValueIs { get; init; } = valueIs;

  public override object Convert(object? value, object? parameter) =>
    ValueIs switch {
      ValueIs.Null => value == null,
      ValueIs.Empty => (value is string s && string.IsNullOrEmpty(s)) || (value is null or IList { Count: 0 }),
      _ => Binding.DoNothing
    };
}