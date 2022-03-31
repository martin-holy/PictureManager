using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MH.UI.WPF.Controls {
  public class WheelSpeedScrollViewer : ScrollViewer {
    public static readonly DependencyProperty SpeedFactorProperty = DependencyProperty.Register(
      nameof(SpeedFactor),
      typeof(double),
      typeof(WheelSpeedScrollViewer),
      new(1.0));

    public double SpeedFactor {
      get => (double)GetValue(SpeedFactorProperty);
      set => SetValue(SpeedFactorProperty, value);
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e) {
      if (e.Handled || ScrollInfo is not VirtualizingStackPanel vsp ||
          ComputedVerticalScrollBarVisibility != Visibility.Visible) return;

      vsp.SetVerticalOffset(VerticalOffset - (e.Delta * SpeedFactor));
      e.Handled = true;
    }
  }
}
