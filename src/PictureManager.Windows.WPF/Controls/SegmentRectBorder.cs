using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PictureManager.Windows.WPF.Controls;

public sealed class SegmentRectBorder : Border {
  public static readonly DependencyProperty IsMouseOver2Property = DependencyProperty.Register(
    nameof(IsMouseOver2), typeof(bool), typeof(SegmentRectBorder));

  public bool IsMouseOver2 {
    get => (bool)GetValue(IsMouseOver2Property);
    set => SetValue(IsMouseOver2Property, value);
  }

  protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters) {
    var hitPoint = hitTestParameters.HitPoint;
    var w = ActualWidth;
    var h = ActualHeight;

    if (w < 20 || h < 20) {
      IsMouseOver2 = true;
      return null;
    }

    var borderRect = new Rect(0, 0, w, h);
    var moveBtnRect = new Rect((w / 2) - 10, (h / 2) - 10, 20, 20);
    var innerRect = new Rect(
      borderRect.X + 10,
      borderRect.Y + 10,
      borderRect.Width - 20,
      borderRect.Height - 20
    );

    if (innerRect.Contains(hitPoint) || moveBtnRect.Contains(hitPoint)) {
      IsMouseOver2 = true;
      return null;
    }

    IsMouseOver2 = false;
    return base.HitTestCore(hitTestParameters);
  }
}