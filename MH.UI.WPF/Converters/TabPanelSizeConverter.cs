using System.Windows.Controls;
using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public class TabPanelSizeConverter : BaseMultiConverter {
  private static readonly object _lock = new();
  private static TabPanelSizeConverter _inst;
  public static TabPanelSizeConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object[] values, object parameter) {
    if (values.Length == 4
        && values[0] is MH.UI.Controls.TabControl control
        && values[1] is Dock placement
        && values[2] is double width
        && values[3] is double height)
      control.TabMaxSize = placement is Dock.Top or Dock.Bottom
        ? width
        : height;

    return Binding.DoNothing;
  }
}