using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MH.UI.WPF.Controls {
  public class PopupSlider: Slider {
    public static readonly DependencyProperty IconProperty =
      DependencyProperty.Register(nameof(Icon), typeof(PathGeometry), typeof(PopupSlider));
    
    public PathGeometry Icon {
      get => (PathGeometry)GetValue(IconProperty);
      set => SetValue(IconProperty, value);
    }

    static PopupSlider() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(PopupSlider),
        new FrameworkPropertyMetadata(typeof(PopupSlider)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      if (Effect == null) return;
      (Template.FindName("PART_IconTextButton", this) as UIElement).Effect = Effect;
      Effect = null;
    }
  }
}
