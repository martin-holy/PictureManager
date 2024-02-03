using MH.UI.WPF.AttachedProperties;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Extensions;
using MH.Utils.BaseClasses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MH.UI.WPF.Converters;

public class RelayCommandConverter : BaseMultiConverter {
  private static readonly object _lock = new();
  private static RelayCommandConverter _inst;
  public static RelayCommandConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object[] values, object parameter) {
    if (values is not [FrameworkElement fe, RelayCommand rc]) return Binding.DoNothing;
    SetIconData(fe, rc);
    SetText(fe, rc);

    return Binding.DoNothing;
  }

  private static void SetIconData(FrameworkElement fe, RelayCommand rc) {
    if (string.IsNullOrEmpty(rc.Icon)) return;

    switch (fe) {
      case IconButton:
        if (!fe.HasAttachedProperty(Icon.DataProperty) || fe.GetValue(Icon.DataProperty) == null)
          fe.SetValue(Icon.DataProperty, ResourceConverter.Inst.Convert(rc.Icon, null));
        break;
      case MenuItem mi:
        mi.Icon ??= GetIcon(rc.Icon);
        break;
    }
  }

  private static void SetText(FrameworkElement fe, RelayCommand rc) {
    if (string.IsNullOrEmpty(rc.Text)) return;

    switch (fe) {
      case IconButton ib: ib.ToolTip ??= rc.Text; break;
      case MenuItem mi: mi.Header ??= rc.Text; break;
    }
  }

  private static Path GetIcon(string icon) =>
    new() {
      Data = (Geometry)ResourceConverter.Inst.Convert(icon, null),
      Style = (Style)ResourceConverter.Inst.Convert("MH.Styles.IconWithShadow", null)
    };
}