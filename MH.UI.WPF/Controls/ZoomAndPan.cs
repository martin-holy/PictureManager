using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace MH.UI.WPF.Controls {
  public class ZoomAndPan: ContentControl {
    public static readonly DependencyProperty ScaleXProperty =
      DependencyProperty.Register(nameof(ScaleX), typeof(double), typeof(ZoomAndPan));
    public static readonly DependencyProperty ScaleYProperty =
      DependencyProperty.Register(nameof(ScaleY), typeof(double), typeof(ZoomAndPan));
    public static readonly DependencyProperty TransformXProperty =
      DependencyProperty.Register(nameof(TransformX), typeof(double), typeof(ZoomAndPan));
    public static readonly DependencyProperty TransformYProperty =
      DependencyProperty.Register(nameof(TransformY), typeof(double), typeof(ZoomAndPan));
    public static readonly DependencyProperty ContentWidthProperty =
      DependencyProperty.Register(nameof(ContentWidth), typeof(double), typeof(ZoomAndPan));
    public static readonly DependencyProperty ContentHeightProperty =
      DependencyProperty.Register(nameof(ContentHeight), typeof(double), typeof(ZoomAndPan));
    public static readonly DependencyProperty IsZoomedProperty =
      DependencyProperty.Register(nameof(IsZoomed), typeof(bool), typeof(ZoomAndPan));
    public static readonly DependencyProperty MinAnimationDurationProperty =
      DependencyProperty.Register(nameof(MinAnimationDuration), typeof(int), typeof(ZoomAndPan));
    public static readonly DependencyProperty IsAnimationOnProperty =
      DependencyProperty.Register(nameof(IsAnimationOn), typeof(bool), typeof(ZoomAndPan),
        new((o, _) => (o as ZoomAndPan)?.IsAnimationOnChanged()));
    public static readonly DependencyProperty ReScaleToFitProperty =
      DependencyProperty.Register(nameof(ReScaleToFit), typeof(bool), typeof(ZoomAndPan),
        new((o, e) => {
          if ((bool)e.NewValue && o is ZoomAndPan self) {
            self.ReScaleToFit = false;
            self.ScaleToFit();
          }
        }));

    public double ScaleX {
      get => (double)GetValue(ScaleXProperty);
      set => SetValue(ScaleXProperty, value);
    }

    public double ScaleY {
      get => (double)GetValue(ScaleYProperty);
      set => SetValue(ScaleYProperty, value);
    }

    public double TransformX {
      get => (double)GetValue(TransformXProperty);
      set => SetValue(TransformXProperty, value);
    }

    public double TransformY {
      get => (double)GetValue(TransformYProperty);
      set => SetValue(TransformYProperty, value);
    }

    public double ContentWidth {
      get => (double)GetValue(ContentWidthProperty);
      set => SetValue(ContentWidthProperty, value);
    }

    public double ContentHeight {
      get => (double)GetValue(ContentHeightProperty);
      set => SetValue(ContentHeightProperty, value);
    }

    public bool IsZoomed {
      get => (bool)GetValue(IsZoomedProperty);
      set => SetValue(IsZoomedProperty, value);
    }

    public int MinAnimationDuration {
      get => (int)GetValue(MinAnimationDurationProperty);
      set => SetValue(MinAnimationDurationProperty, value);
    }

    public bool IsAnimationOn {
      get => (bool)GetValue(IsAnimationOnProperty);
      set => SetValue(IsAnimationOnProperty, value);
    }

    public bool ReScaleToFit {
      get => (bool)GetValue(ReScaleToFitProperty);
      set => SetValue(ReScaleToFitProperty, value);
    }

    private Canvas _canvas;
    private UIElement _content;
    private TranslateTransform _contentTransform;
    private Point _origin;
    private Point _start;

    static ZoomAndPan() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(ZoomAndPan),
        new FrameworkPropertyMetadata(typeof(ZoomAndPan)));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      _canvas = Template.FindName("PART_Canvas", this) as Canvas;
      _canvas.SizeChanged += Canvas_OnSizeChanged;
      _canvas.MouseMove += Canvas_OnMouseMove;

      _content = Template.FindName("PART_Content", this) as UIElement;
      _content.MouseLeftButtonDown += Content_OnMouseLeftButtonDown;
      _content.MouseLeftButtonUp += Content_OnMouseLeftButtonUp;
      _content.MouseWheel += Content_OnMouseWheel;

      _contentTransform = Template.FindName("PART_ContentTransform", this) as TranslateTransform;
    }

    public void IsAnimationOnChanged() {
      if (IsAnimationOn)
        Play();
      else
        Stop();
    }

    public void Play() {
      var horizontal = ActualHeight / ContentHeight * ContentWidth > ActualWidth;
      var scale = horizontal
        ? ActualHeight / ContentHeight
        : ActualWidth / ContentWidth;

      if (scale > 1) scale = 1;

      var toValue = horizontal
        ? ((ContentWidth * scale) - ActualWidth) * -1
        : ((ContentHeight * scale) - ActualHeight) * -1;

      SetScale(scale, new(ContentWidth / 2, ContentHeight / 2));

      var duration = toValue * 10 * -1 > MinAnimationDuration
        ? toValue * 10 * -1
        : MinAnimationDuration;
      var animation = new DoubleAnimation(0, toValue, TimeSpan.FromMilliseconds(duration), FillBehavior.Stop);

      animation.Completed += (_, _) => {
        if (!IsAnimationOn) return;

        if (horizontal)
          TransformX = toValue;
        else
          TransformY = toValue;

        IsAnimationOn = false;
      };

      _contentTransform.BeginAnimation(horizontal
        ? TranslateTransform.XProperty
        : TranslateTransform.YProperty, animation);
    }

    public void Stop() {
      _contentTransform?.BeginAnimation(TranslateTransform.XProperty, null);
    }

    private void Canvas_OnSizeChanged(object sender, SizeChangedEventArgs e) =>
      ScaleToFit();

    private void Canvas_OnMouseMove(object sender, MouseEventArgs e) {
      if (!_content.IsMouseCaptured) return;
      var v = _start - e.GetPosition(_canvas);
      TransformX = _origin.X - v.X;
      TransformY = _origin.Y - v.Y;
    }

    private void Content_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (!IsZoomed)
        SetScale(1, e.GetPosition(_content));

      _start = e.GetPosition(_canvas);
      _origin = new(TransformX, TransformY);

      _canvas.Cursor = Cursors.Hand;
      _content.CaptureMouse();
    }

    private void Content_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      _canvas.Cursor = Cursors.Arrow;
      _content.ReleaseMouseCapture();

      if (!IsZoomed)
        ScaleToFit();
    }

    private void Content_OnMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == 0
          || (!(e.Delta > 0) && (ScaleX < .2 || ScaleY < .2))) return;

      IsZoomed = true;
      var scale = ScaleX + (e.Delta > 0 ? .1 : -.1);
      SetScale(scale, e.GetPosition(_content));
    }

    private void SetScale(double scale, Point relative) {
      var absoluteX = (relative.X * ScaleX) + TransformX;
      var absoluteY = (relative.Y * ScaleY) + TransformY;
      ScaleX = scale;
      ScaleY = scale;
      TransformX = absoluteX - (relative.X * ScaleX);
      TransformY = absoluteY - (relative.Y * ScaleY);
    }

    public void ScaleToFit() {
      var scale = GetFitScale(ContentWidth, ContentHeight, ActualWidth, ActualHeight);
      ScaleX = scale;
      ScaleY = scale;
      TransformX = (ActualWidth - (ContentWidth * scale)) / 2;
      TransformY = (ActualHeight - (ContentHeight * scale)) / 2;
      IsZoomed = false;
    }

    private static double GetFitScale(double contentW, double contentH, double screenW, double screenH) {
      var cw = contentW;
      var ch = contentH;

      if (cw > screenW) {
        cw = screenW;
        ch = contentH / (contentW / screenW);

        if (ch > screenH) {
          ch = screenH;
          cw = contentW / (contentH / screenH);
        }
      }

      if (ch > screenH) {
        cw = contentW / (contentH / screenH);

        if (cw > screenW)
          cw = screenW;
      }

      return cw / contentW;
    }
  }
}
