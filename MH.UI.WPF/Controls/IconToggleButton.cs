using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MH.UI.WPF.Controls {
  public class IconToggleButton : ToggleButton {
    public static readonly DependencyProperty IconProperty =
      DependencyProperty.Register(nameof(Icon), typeof(PathGeometry), typeof(IconToggleButton));

    public PathGeometry Icon {
      get => (PathGeometry)GetValue(IconProperty);
      set => SetValue(IconProperty, value);
    }

    static IconToggleButton() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(IconToggleButton),
        new FrameworkPropertyMetadata(typeof(IconToggleButton)));
    }
  }
}
