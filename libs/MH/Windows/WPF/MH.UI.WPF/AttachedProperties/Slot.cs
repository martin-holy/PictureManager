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
  public static readonly DependencyProperty LeftContentTemplateProperty = DependencyProperty.RegisterAttached(
    "LeftContentTemplate", typeof(DataTemplate), typeof(Slot));
  public static readonly DependencyProperty TopContentTemplateProperty = DependencyProperty.RegisterAttached(
    "TopContentTemplate", typeof(DataTemplate), typeof(Slot));
  public static readonly DependencyProperty RightContentTemplateProperty = DependencyProperty.RegisterAttached(
    "RightContentTemplate", typeof(DataTemplate), typeof(Slot));
  public static readonly DependencyProperty BottomContentTemplateProperty = DependencyProperty.RegisterAttached(
    "BottomContentTemplate", typeof(DataTemplate), typeof(Slot));

  public static object GetLeftContent(DependencyObject d) => d.GetValue(LeftContentProperty);
  public static void SetLeftContent(DependencyObject d, object value) => d.SetValue(LeftContentProperty, value);
  public static object GetTopContent(DependencyObject d) => d.GetValue(TopContentProperty);
  public static void SetTopContent(DependencyObject d, object value) => d.SetValue(TopContentProperty, value);
  public static object GetRightContent(DependencyObject d) => d.GetValue(RightContentProperty);
  public static void SetRightContent(DependencyObject d, object value) => d.SetValue(RightContentProperty, value);
  public static object GetBottomContent(DependencyObject d) => d.GetValue(BottomContentProperty);
  public static void SetBottomContent(DependencyObject d, object value) => d.SetValue(BottomContentProperty, value);
  public static DataTemplate GetLeftContentTemplate(DependencyObject d) => (DataTemplate)d.GetValue(LeftContentTemplateProperty);
  public static void SetLeftContentTemplate(DependencyObject d, DataTemplate value) => d.SetValue(LeftContentTemplateProperty, value);
  public static DataTemplate GetTopContentTemplate(DependencyObject d) => (DataTemplate)d.GetValue(TopContentTemplateProperty);
  public static void SetTopContentTemplate(DependencyObject d, DataTemplate value) => d.SetValue(TopContentTemplateProperty, value);
  public static DataTemplate GetRightContentTemplate(DependencyObject d) => (DataTemplate)d.GetValue(RightContentTemplateProperty);
  public static void SetRightContentTemplate(DependencyObject d, DataTemplate value) => d.SetValue(RightContentTemplateProperty, value);
  public static DataTemplate GetBottomContentTemplate(DependencyObject d) => (DataTemplate)d.GetValue(BottomContentTemplateProperty);
  public static void SetBottomContentTemplate(DependencyObject d, DataTemplate value) => d.SetValue(BottomContentTemplateProperty, value);
}