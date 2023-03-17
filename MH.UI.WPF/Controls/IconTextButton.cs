using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MH.UI.WPF.Controls {
  public class IconTextButton : Button {
    public static readonly DependencyProperty IconProperty =
      DependencyProperty.Register(nameof(Icon), typeof(PathGeometry), typeof(IconTextButton));
    public static readonly DependencyProperty TextProperty =
      DependencyProperty.Register(nameof(Text), typeof(string), typeof(IconTextButton));

    public PathGeometry Icon {
      get => (PathGeometry)GetValue(IconProperty);
      set => SetValue(IconProperty, value);
    }

    public string Text {
      get => (string)GetValue(TextProperty);
      set => SetValue(TextProperty, value);
    }

    static IconTextButton() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(IconTextButton),
        new FrameworkPropertyMetadata(typeof(IconTextButton)));
    }
  }
}
