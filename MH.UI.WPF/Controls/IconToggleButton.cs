using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MH.UI.WPF.Controls {
  public class IconToggleButton : ToggleButton {
    public static readonly DependencyProperty SourceProperty = Icon.SourceProperty.AddOwner(typeof(IconToggleButton));
    public static readonly DependencyProperty FillProperty = Icon.FillProperty.AddOwner(typeof(IconToggleButton));
    public static readonly DependencyProperty SizeProperty = Icon.SizeProperty.AddOwner(typeof(IconToggleButton));
    public static readonly DependencyProperty ShowShadowProperty = Icon.ShowShadowProperty.AddOwner(typeof(IconToggleButton), new PropertyMetadata(true));

    public PathGeometry Source {
      get => (PathGeometry)GetValue(SourceProperty);
      set => SetValue(SourceProperty, value);
    }

    public Brush Fill {
      get => (Brush)GetValue(FillProperty);
      set => SetValue(FillProperty, value);
    }

    public double Size {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public bool ShowShadow {
      get => (bool)GetValue(ShowShadowProperty);
      set => SetValue(ShowShadowProperty, value);
    }

    static IconToggleButton() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(IconToggleButton), new FrameworkPropertyMetadata(typeof(IconToggleButton)));
    }
  }
}
