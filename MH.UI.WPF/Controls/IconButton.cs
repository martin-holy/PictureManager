using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MH.UI.WPF.Controls {
  public class IconButton : Button {
    public static readonly DependencyProperty IconProperty =
      DependencyProperty.Register(nameof(Icon), typeof(PathGeometry), typeof(IconButton));

    public PathGeometry Icon {
      get => (PathGeometry)GetValue(IconProperty);
      set => SetValue(IconProperty, value);
    }

    static IconButton() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(IconButton),
        new FrameworkPropertyMetadata(typeof(IconButton)));
    }
  }
}
