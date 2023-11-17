using System.Windows;

namespace MH.UI.WPF.AttachedProperties; 

public static class Text {
  public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
    "Text", typeof(string), typeof(Text));

  public static string GetText(DependencyObject d) => (string)d.GetValue(TextProperty);
  public static void SetText(DependencyObject d, string value) => d.SetValue(TextProperty, value);
}