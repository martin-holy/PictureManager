using System.Windows;

namespace MH.UI.WPF.AttachedProperties;

public static class Button {
  public static readonly DependencyProperty ShowBorderProperty = DependencyProperty.RegisterAttached(
    "ShowBorder", typeof(bool), typeof(Button));

  public static bool GetShowBorder(DependencyObject d) => (bool)d.GetValue(ShowBorderProperty);
  public static void SetShowBorder(DependencyObject d, bool value) => d.SetValue(ShowBorderProperty, value);
}