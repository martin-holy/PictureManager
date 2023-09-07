using System.Windows.Controls;
using System.Windows.Data;
using TabControl = MH.UI.Controls.TabControl;

namespace MH.UI.WPF.Converters {
  public class TabPanelSizeConverter : BaseMarkupExtensionMultiConverter {
    public override object Convert(object[] values, object parameter) {
      if (values.Length == 4
          && values[0] is TabControl control
          && values[1] is Dock placement
          && values[2] is double width
          && values[3] is double height)
        control.TabMaxSize = placement is Dock.Top or Dock.Bottom
          ? width
          : height;

      return Binding.DoNothing;
    }
  }
}
