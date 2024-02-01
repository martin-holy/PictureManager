using MH.Utils.BaseClasses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MH.UI.WPF.Converters;

public class RelayCommandConverter : BaseConverter {
  private static readonly object _lock = new();
  private static RelayCommandConverter _inst;
  public static RelayCommandConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object value, object parameter) {
    if (value is not MenuItem { Command: RelayCommand command } mi) return false;

    if (!string.IsNullOrEmpty(command.Icon))
      mi.Icon = GetIcon(command.Icon);

    if (!string.IsNullOrEmpty(command.Text))
      mi.Header = command.Text;

    return false;
  }

  private static Path GetIcon(string icon) =>
    new() {
      Data = (Geometry)ResourceConverter.Inst.Convert(icon, null),
      Style = (Style)ResourceConverter.Inst.Convert("MH.Styles.IconWithShadow", null)
    };
}