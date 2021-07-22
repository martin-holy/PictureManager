using PictureManager.Domain;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PictureManager.CustomControls {
  public class IconRect : Control {
    public static readonly DependencyProperty IconNameProperty = DependencyProperty.Register(nameof(IconName), typeof(IconName), typeof(IconRect));
    public static readonly DependencyProperty IconFillProperty = DependencyProperty.Register(nameof(IconFill), typeof(Brush), typeof(IconRect));
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(nameof(Size), typeof(double), typeof(IconRect), new PropertyMetadata(18.0));
    public static readonly DependencyProperty IsIconVisibleProperty = DependencyProperty.Register(nameof(IsIconVisible), typeof(bool), typeof(IconRect), new PropertyMetadata(true));

    public IconName IconName {
      get => (IconName)GetValue(IconNameProperty);
      set => SetValue(IconNameProperty, value);
    }

    public Brush IconFill {
      get => (Brush)GetValue(IconFillProperty);
      set => SetValue(IconFillProperty, value);
    }

    public double Size {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    public bool IsIconVisible {
      get => (bool)GetValue(IsIconVisibleProperty);
      set => SetValue(IsIconVisibleProperty, value);
    }

    static IconRect() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(IconRect), new FrameworkPropertyMetadata(typeof(IconRect)));
    }
  }
}
