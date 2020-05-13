using System.Windows;
using System.Windows.Controls;
using PictureManager.Domain;

namespace PictureManager.CustomControls {
  public class IconRect: Control {
    public static readonly DependencyProperty IconNameProperty = DependencyProperty.Register(
      nameof(IconName), typeof(IconName), typeof(IconRect));

    public IconName IconName {
      get => (IconName)GetValue(IconNameProperty);
      set => SetValue(IconNameProperty, value);
    }

    static IconRect() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(IconRect),
        new FrameworkPropertyMetadata(typeof(IconRect)));
    }
  }
}
