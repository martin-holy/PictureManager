using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MH.UI.WPF.Controls {
  public class IconButton : Button {
    public static readonly DependencyProperty SourceProperty = Icon.SourceProperty.AddOwner(typeof(IconButton));
    public static readonly DependencyProperty FillProperty = Icon.FillProperty.AddOwner(typeof(IconButton));
    public static readonly DependencyProperty SizeProperty = Icon.SizeProperty.AddOwner(typeof(IconButton));
    public static readonly DependencyProperty ShowShadowProperty = Icon.ShowShadowProperty.AddOwner(typeof(IconButton), new PropertyMetadata(true));

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

    static IconButton() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(IconButton), new FrameworkPropertyMetadata(typeof(IconButton)));
    }
  }
}
