using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public class MaxTabSizeConverter : BaseMultiConverter {
  private static readonly object _lock = new();
  private static MaxTabSizeConverter? _inst;
  public static MaxTabSizeConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object[]? values, object? parameter) {
    if (values?.Length != 3) return Binding.DoNothing;

    (values[0] as MH.UI.Controls.TabControl)?.UpdateMaxTabSize(values[1] as double?, values[2] as double?);

    return Binding.DoNothing;
  }
}