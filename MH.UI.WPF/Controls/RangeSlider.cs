/* based on https://github.com/funwaywang/WpfRangeSlider */
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MH.UI.WPF.Controls {
  public class RangeSlider : Control {
    private FrameworkElement _sliderContainer;
    private Thumb _startThumb, _endThumb;
    private FrameworkElement _startArea;
    private FrameworkElement _selectedArea;
    private FrameworkElement _endArea;

    enum SliderThumb {
      None,
      Start,
      End
    }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
      nameof(Maximum), typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
      nameof(Minimum), typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty StartProperty = DependencyProperty.Register(
      nameof(Start), typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty EndProperty = DependencyProperty.Register(
      nameof(End), typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
      nameof(Orientation), typeof(Orientation), typeof(RangeSlider), new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static readonly DependencyProperty IsMoveToPointEnabledProperty = DependencyProperty.Register(
      nameof(IsMoveToPointEnabled), typeof(bool), typeof(RangeSlider), new FrameworkPropertyMetadata(true));
    public static readonly DependencyProperty TickFrequencyProperty = DependencyProperty.Register(
      nameof(TickFrequency), typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(1d));

    public Orientation Orientation {
      get => (Orientation)GetValue(OrientationProperty);
      set => SetValue(OrientationProperty, value);
    }

    public double Maximum {
      get => (double)GetValue(MaximumProperty);
      set => SetValue(MaximumProperty, value);
    }

    public double Minimum {
      get => (double)GetValue(MinimumProperty);
      set => SetValue(MinimumProperty, value);
    }

    public double Start {
      get => (double)GetValue(StartProperty);
      set => SetValue(StartProperty, value);
    }

    public double End {
      get => (double)GetValue(EndProperty);
      set => SetValue(EndProperty, value);
    }

    public bool IsMoveToPointEnabled {
      get => (bool)GetValue(IsMoveToPointEnabledProperty);
      set => SetValue(IsMoveToPointEnabledProperty, value);
    }

    public double TickFrequency {
      get => (double)GetValue(TickFrequencyProperty);
      set => SetValue(TickFrequencyProperty, value);
    }

    public event EventHandler SelectionChangedEventHandler = delegate { };

    static RangeSlider() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(RangeSlider),
        new FrameworkPropertyMetadata(typeof(RangeSlider)));

      EventManager.RegisterClassHandler(typeof(RangeSlider), Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnThumbDragDelta));
      EventManager.RegisterClassHandler(typeof(RangeSlider), Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnDragCompletedEvent));
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      _sliderContainer = GetTemplateChild("PART_SliderContainer") as FrameworkElement;
      _sliderContainer.PreviewMouseDown += ViewBox_PreviewMouseDown;
      _startArea = GetTemplateChild("PART_StartArea") as FrameworkElement;
      _selectedArea = GetTemplateChild("PART_SelectedArea") as FrameworkElement;
      _endArea = GetTemplateChild("PART_EndArea") as FrameworkElement;
      _startThumb = GetTemplateChild("PART_StartThumb") as Thumb;
      _endThumb = GetTemplateChild("PART_EndThumb") as Thumb;
    }

    protected override Size ArrangeOverride(Size arrangeBounds) {
      var arrangeSize = base.ArrangeOverride(arrangeBounds);

      if (Maximum > Minimum && _startArea != null && _selectedArea != null && _endArea != null) {
        var start = Math.Max(Minimum, Math.Min(Maximum, Start));
        var end = Math.Max(Minimum, Math.Min(Maximum, End));
        Rect rectStart, rectSelected, rectEnd;

        if (Orientation == Orientation.Horizontal) {
          var viewportSize = _sliderContainer != null ? _sliderContainer.ActualWidth : arrangeBounds.Width;
          var startPosition = (start - Minimum) / (Maximum - Minimum) * viewportSize;
          var endPosition = (end - Minimum) / (Maximum - Minimum) * viewportSize;
          rectStart = new Rect(0, 0, startPosition, arrangeBounds.Height);
          rectSelected = new Rect(startPosition, 0, endPosition - startPosition, arrangeBounds.Height);
          rectEnd = new Rect(endPosition, 0, viewportSize - endPosition, arrangeBounds.Height);
        }
        else {
          var viewportSize = _sliderContainer != null ? _sliderContainer.ActualHeight : arrangeBounds.Height;
          var startPosition = (start - Minimum) / (Maximum - Minimum) * viewportSize;
          var endPosition = (end - Minimum) / (Maximum - Minimum) * viewportSize;
          rectStart = new Rect(0, 0, arrangeBounds.Width, startPosition);
          rectSelected = new Rect(0, startPosition, arrangeBounds.Width, endPosition - startPosition);
          rectEnd = new Rect(0, endPosition, arrangeBounds.Width, viewportSize - endPosition);
        }

        _startArea.Arrange(rectStart);
        _selectedArea.Arrange(rectSelected);
        _endArea.Arrange(rectEnd);
        _startThumb.Arrange(rectStart);
        _endThumb.Arrange(rectEnd);
      }

      return arrangeSize;
    }

    private void ViewBox_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      if (!IsMoveToPointEnabled || _startThumb.IsMouseOver || _endThumb.IsMouseOver) return;

      var point = e.GetPosition(_sliderContainer);
      if (e.ChangedButton == MouseButton.Left)
        MoveBlockTo(point, SliderThumb.Start);
      else if (e.ChangedButton == MouseButton.Right)
        MoveBlockTo(point, SliderThumb.End);

      e.Handled = true;
    }

    private void MoveBlockTo(Point point, SliderThumb block) {
      double position = Orientation == Orientation.Horizontal
        ? point.X
        : point.Y;

      double viewportSize = Orientation == Orientation.Horizontal
        ? _sliderContainer.ActualWidth
        : _sliderContainer.ActualHeight;

      if (!double.IsNaN(viewportSize) && viewportSize > 0) {
        var value = Round(Math.Min(Maximum, Minimum + (position / viewportSize) * (Maximum - Minimum)));
        
        if (block == SliderThumb.Start)
          Start = Math.Min(End, value);
        else if (block == SliderThumb.End)
          End = Math.Max(Start, value);
      }
    }

    private static void OnThumbDragDelta(object sender, DragDeltaEventArgs e) {
      if (sender is RangeSlider rs)
        rs.OnThumbDragDelta(e);
    }

    private void OnThumbDragDelta(DragDeltaEventArgs e) {
      if (e.OriginalSource is Thumb thumb && _sliderContainer != null) {
        double change = Orientation == Orientation.Horizontal
          ? e.HorizontalChange / _sliderContainer.ActualWidth * (Maximum - Minimum)
          : e.VerticalChange / _sliderContainer.ActualHeight * (Maximum - Minimum);

        if (thumb == _startThumb)
          Start = Round(Math.Max(Minimum, Math.Min(End, Start + change)));
        else if (thumb == _endThumb)
          End = Round(Math.Min(Maximum, Math.Max(Start, End + change)));
      }
    }

    private static void OnDragCompletedEvent(object sender, DragCompletedEventArgs e) {
      if (sender is RangeSlider rs)
        rs.OnDragCompletedEvent(e);
    }

    private void OnDragCompletedEvent(DragCompletedEventArgs e) {
      SelectionChangedEventHandler(this, EventArgs.Empty);
    }

    private double Round(double value) {
      var precision = 0;
      while (TickFrequency * Math.Pow(10, precision) != Math.Round(TickFrequency * Math.Pow(10, precision)))
        precision++;

      return Math.Round(Math.Round(value / TickFrequency, 0) * TickFrequency, precision);
    }
  }
}