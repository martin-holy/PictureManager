using MH.UI.WPF.Controls;
using MH.UI.WPF.Extensions;
using MH.Utils.BaseClasses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using AP = MH.UI.WPF.AttachedProperties;

namespace MH.UI.WPF.Converters;

public class RelayCommandConverter : BaseMultiConverter {
  private static readonly object _lock = new();
  private static RelayCommandConverter? _inst;
  public static RelayCommandConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object? Convert(object?[]? values, object? parameter) {
    if (values is not [FrameworkElement fe, RelayCommandBase rc]) return Binding.DoNothing;
    SetIconData(fe, rc);
    SetText(fe, rc);

    return Binding.DoNothing;
  }

  private static void SetIconData(FrameworkElement fe, RelayCommandBase rc) {
    if (string.IsNullOrEmpty(rc.Icon)) return;

    switch (fe) {
      case Button:
        if (!fe.HasAttachedProperty(AP.Icon.DataProperty) || fe.GetValue(AP.Icon.DataProperty) == null)
          fe.SetValue(AP.Icon.DataProperty, ResourceConverter.Inst.Convert(rc.Icon, null));
        if (!fe.HasAttachedProperty(AP.Icon.FillProperty) || fe.GetValue(AP.Icon.FillProperty) == null)
          fe.SetValue(AP.Icon.FillProperty, ResourceConverter.Inst.Convert(rc.Icon, Resources.Dictionaries.IconToBrush));
        break;
      case MenuItem mi:
        mi.Icon ??= GetIcon(rc.Icon);
        break;
    }
  }

  private static void SetText(FrameworkElement fe, RelayCommandBase rc) {
    if (string.IsNullOrEmpty(rc.Text)) return;

    switch (fe) {
      case IconButton:
      case IconTextButton:
      case SlimButton:
      case IconToggleButton:
        ((Button)fe).ToolTip ??= rc.Text;
        break;
      case Button:
        fe.SetValue(AP.Text.TextProperty, rc.Text);
        break;
      case MenuItem mi: mi.Header ??= rc.Text; break;
    }
  }

  private static Path GetIcon(string icon) =>
    new() {
      Data = ResourceConverter.Inst.Convert(icon, null) as Geometry,
      Fill = ResourceConverter.Inst.Convert(icon, Resources.Dictionaries.IconToBrush) as Brush,
      Style = ResourceConverter.Inst.Convert("MH.S.Icon.Shadow", null) as Style
    };
}