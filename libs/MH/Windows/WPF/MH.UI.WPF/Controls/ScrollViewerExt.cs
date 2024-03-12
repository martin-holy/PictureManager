using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Controls;

public class ScrollViewerExt : ScrollViewer {
  public static readonly DependencyProperty VerticalTopContentProperty = DependencyProperty.Register(
    nameof(VerticalTopContent), typeof(object), typeof(ScrollViewerExt));

  public object VerticalTopContent {
    get => GetValue(VerticalTopContentProperty);
    set => SetValue(VerticalTopContentProperty, value);
  }
}