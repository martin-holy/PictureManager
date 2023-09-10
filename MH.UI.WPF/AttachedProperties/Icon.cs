using System.Windows;
using System.Windows.Media;

namespace MH.UI.WPF.AttachedProperties; 

public static class Icon {
  public static readonly DependencyProperty DataProperty = DependencyProperty.RegisterAttached(
    "Data", typeof(PathGeometry), typeof(Icon));
  public static readonly DependencyProperty FillProperty = DependencyProperty.RegisterAttached(
    "Fill", typeof(Brush), typeof(Icon));
  public static readonly DependencyProperty SizeProperty = DependencyProperty.RegisterAttached(
    "Data", typeof(double), typeof(Icon));

  public static PathGeometry GetData(DependencyObject d) => (PathGeometry)d.GetValue(DataProperty);
  public static void SetData(DependencyObject d, PathGeometry value) => d.SetValue(DataProperty, value);
  public static Brush GetFill(DependencyObject d) => (Brush)d.GetValue(FillProperty);
  public static void SetFill(DependencyObject d, Brush value) => d.SetValue(FillProperty, value);
  public static double GetSize(DependencyObject d) => (double)d.GetValue(SizeProperty);
  public static void SetSize(DependencyObject d, double value) => d.SetValue(SizeProperty, value);
}