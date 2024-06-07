using System.Windows;

namespace MH.UI.WPF.AttachedProperties; 

public static class Slot {
  public static readonly DependencyProperty LeftContentProperty = DependencyProperty.RegisterAttached(
    "LeftContent", typeof(object), typeof(Slot));
  public static readonly DependencyProperty TopContentProperty = DependencyProperty.RegisterAttached(
    "TopContent", typeof(object), typeof(Slot));
  public static readonly DependencyProperty RightContentProperty = DependencyProperty.RegisterAttached(
    "RightContent", typeof(object), typeof(Slot));
  public static readonly DependencyProperty BottomContentProperty = DependencyProperty.RegisterAttached(
    "BottomContent", typeof(object), typeof(Slot));

  public static object GetTopContent(DependencyObject d) => d.GetValue(TopContentProperty);
  public static void SetTopContent(DependencyObject d, object value) => d.SetValue(TopContentProperty, value);
  public static object GetLeftContent(DependencyObject d) => d.GetValue(LeftContentProperty);
  public static void SetLeftContent(DependencyObject d, object value) => d.SetValue(LeftContentProperty, value);
  public static object GetRightContent(DependencyObject d) => d.GetValue(RightContentProperty);
  public static void SetRightContent(DependencyObject d, object value) => d.SetValue(RightContentProperty, value);
  public static object GetBottomContent(DependencyObject d) => d.GetValue(BottomContentProperty);
  public static void SetBottomContent(DependencyObject d, object value) => d.SetValue(BottomContentProperty, value);
}