using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      if (Effect == null) return;
      (Template.FindName("PART_Icon", this) as UIElement).Effect = Effect;
      (Template.FindName("PART_Text", this) as UIElement).Effect = Effect;
      Effect = null;
    }
  }
}
