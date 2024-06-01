using System.Windows;

namespace MH.UI.WPF.AttachedProperties; 

public static class Text {
  public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
    "Text", typeof(string), typeof(Text));
  public static readonly DependencyProperty ShadowProperty = DependencyProperty.RegisterAttached(
    "Shadow", typeof(bool), typeof(Text));

  public static string GetText(DependencyObject d) => (string)d.GetValue(TextProperty);
  public static void SetText(DependencyObject d, string value) => d.SetValue(TextProperty, value);
  public static bool GetShadow(DependencyObject d) => (bool)d.GetValue(ShadowProperty);
  public static void SetShadow(DependencyObject d, bool value) => d.SetValue(ShadowProperty, value);
}