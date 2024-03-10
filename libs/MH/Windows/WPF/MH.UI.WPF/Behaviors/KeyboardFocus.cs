using System.Windows;
using System.Windows.Input;

namespace MH.UI.WPF.Behaviors;

public static class KeyboardFocus {
  public static readonly DependencyProperty OnProperty = DependencyProperty.RegisterAttached(
    "On", typeof(FrameworkElement), typeof(KeyboardFocus), new(OnSetChanged));

  public static void SetOn(UIElement element, FrameworkElement value) =>
    element.SetValue(OnProperty, value);

  public static FrameworkElement GetOn(UIElement element) =>
    (FrameworkElement)element.GetValue(OnProperty);

  private static void OnSetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    var fe = (FrameworkElement)d;
    if (GetOn(fe) is not { } target) return;
    fe.Loaded += (_, _) => Keyboard.Focus(target);
  }
}