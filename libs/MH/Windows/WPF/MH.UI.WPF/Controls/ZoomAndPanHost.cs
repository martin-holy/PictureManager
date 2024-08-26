using MH.UI.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MH.UI.WPF.Controls;

public class ZoomAndPanHost : ContentControl, IZoomAndPanHost {
  private Canvas _canvas = null!;
  private UIElement _content = null!;
  private TranslateTransform _contentTransform = null!;

  public static readonly DependencyProperty ZoomAndPanProperty = DependencyProperty.Register(
    nameof(ZoomAndPan), typeof(ZoomAndPan), typeof(ZoomAndPanHost), new(_onZoomAndPanChanged));

  public ZoomAndPan ZoomAndPan {
    get => (ZoomAndPan)GetValue(ZoomAndPanProperty);
    set => SetValue(ZoomAndPanProperty, value);
  }

  double IZoomAndPanHost.Width => ActualWidth;
  double IZoomAndPanHost.Height => ActualHeight;

  public override void OnApplyTemplate() {
    base.OnApplyTemplate();

    _canvas = (Canvas)GetTemplateChild("PART_Canvas")!;
    _canvas.SizeChanged += _onCanvasSizeChanged;
    _canvas.MouseMove += _onCanvasMouseMove;

    _content = (UIElement)GetTemplateChild("PART_Content")!;
    _content.MouseLeftButtonDown += _onContentMouseLeftButtonDown;
    _content.MouseLeftButtonUp += _onContentMouseLeftButtonUp;
    _content.MouseWheel += _onContentMouseWheel;

    _contentTransform = (TranslateTransform)GetTemplateChild("PART_ContentTransform")!;
  }

  public void StartAnimation(double toValue, double duration, bool horizontal, Action onCompleted) {
    var animation = new DoubleAnimation(0, toValue, TimeSpan.FromMilliseconds(duration), FillBehavior.Stop);
    animation.Completed += (_, _) => onCompleted();
    _contentTransform.BeginAnimation(horizontal
      ? TranslateTransform.XProperty
      : TranslateTransform.YProperty, animation);
  }

  public void StopAnimation() =>
    _contentTransform.BeginAnimation(TranslateTransform.XProperty, null);

  private void _onCanvasSizeChanged(object sender, SizeChangedEventArgs e) =>
    ZoomAndPan.OnHostSizeChanged();

  private void _onCanvasMouseMove(object sender, MouseEventArgs e) {
    if (!_content.IsMouseCaptured) return;
    var hostPos = e.GetPosition(_canvas);
    ZoomAndPan.OnHostMouseMove(hostPos.X, hostPos.Y);
  }

  private void _onContentMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
    var hostPos = e.GetPosition(_canvas);
    var contentPos = e.GetPosition(_content);
    ZoomAndPan.OnContentMouseDown(hostPos.X, hostPos.Y, contentPos.X, contentPos.Y);
    _canvas.Cursor = Cursors.Hand;
    _content.CaptureMouse();
  }

  private void _onContentMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
    _canvas.Cursor = Cursors.Arrow;
    _content.ReleaseMouseCapture();
    ZoomAndPan.OnContentMouseUp();
  }

  private void _onContentMouseWheel(object sender, MouseWheelEventArgs e) {
    var contentPos = e.GetPosition(_content);
    ZoomAndPan.OnContentMouseWheel(e.Delta, contentPos.X, contentPos.Y);
  }

  private static void _onZoomAndPanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    if (d is not ZoomAndPanHost host) return;
    host.ZoomAndPan.Host = host;
  }
}