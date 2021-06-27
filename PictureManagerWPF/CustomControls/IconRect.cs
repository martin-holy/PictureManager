using PictureManager.Domain;
using System.Windows;
using System.Windows.Controls;

namespace PictureManager.CustomControls {
  public class IconRect : Control {
    public static readonly DependencyProperty IconNameProperty = DependencyProperty.Register(nameof(IconName), typeof(IconName), typeof(IconRect));
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(nameof(Size), typeof(double), typeof(IconRect), new PropertyMetadata(18.0));

    public IconName IconName {
      get => (IconName)GetValue(IconNameProperty);
      set => SetValue(IconNameProperty, value);
    }

    public double Size {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    static IconRect() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(IconRect), new FrameworkPropertyMetadata(typeof(IconRect)));
    }
  }
}
