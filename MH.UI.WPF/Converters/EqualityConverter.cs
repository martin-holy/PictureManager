namespace MH.UI.WPF.Converters;

public class EqualityConverter : BaseMultiConverter {
  private static readonly object _lock = new();
  private static EqualityConverter _inst;
  public static EqualityConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object[] values, object parameter) {
    if (values is [double a, string op, double b])
      switch (op.ToLower()) {
        case "eq": return a == b;
        case "ne": return a != b;
        case "gt": return a > b;
        case "ge": return a >= b;
        case "lt": return a < b;
        case "le": return a <= b;
      }

    return false;
  }
}