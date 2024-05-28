using System.Windows;
using System.Windows.Media;

namespace MH.UI.WPF.AttachedProperties; 

public static class Button {
  public static readonly DependencyProperty OverLayerProperty = DependencyProperty.RegisterAttached(
    "OverLayer", typeof(Brush), typeof(Button));

  public static Brush GetOverLayer(DependencyObject d) => (Brush)d.GetValue(OverLayerProperty);
  public static void SetOverLayer(DependencyObject d, Brush value) => d.SetValue(OverLayerProperty, value);
}