using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MH.UI.WPF.Controls;

public class RangeSlider : Control {
  private FrameworkElement _sliderContainer;
  private FrameworkElement _startArea;
  private FrameworkElement _selectedArea;
  private FrameworkElement _endArea;
  private Thumb _startThumb, _endThumb;

  public static readonly DependencyProperty RangeProperty = DependencyProperty.Register(
    nameof(Range), typeof(SelectionRange), typeof(RangeSlider),
    new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure, OnRangePropertyChanged));
  public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
    nameof(Orientation), typeof(Orientation), typeof(RangeSlider), new(Orientation.Horizontal));
  public static readonly DependencyProperty TickFrequencyProperty = DependencyProperty.Register(
    nameof(TickFrequency), typeof(double), typeof(RangeSlider), new(1d));

  public SelectionRange Range { get => (SelectionRange)GetValue(RangeProperty); set => SetValue(RangeProperty, value); }
  public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
  public double TickFrequency { get => (double)GetValue(TickFrequencyProperty); set => SetValue(TickFrequencyProperty, value); }

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
    if (_sliderContainer != null) _sliderContainer.PreviewMouseDown += OnSliderPreviewMouseDown;
    _startArea = GetTemplateChild("PART_StartArea") as FrameworkElement;
    _selectedArea = GetTemplateChild("PART_SelectedArea") as FrameworkElement;
    _endArea = GetTemplateChild("PART_EndArea") as FrameworkElement;
    _startThumb = GetTemplateChild("PART_StartThumb") as Thumb;
    _endThumb = GetTemplateChild("PART_EndThumb") as Thumb;
  }

  protected override Size ArrangeOverride(Size bounds) {
    var arrangeSize = base.ArrangeOverride(bounds);
    if (_startArea == null || _selectedArea == null || _endArea == null) return arrangeSize;

    var size = _sliderContainer?.ActualWidth ?? bounds.Width;
    var start = Range == null || Range.Start == 0 ? 0 : (Range.Start - Range.Min) / (Range.Max - Range.Min) * size;
    var end = Range == null || Range.End == 0 ? size : (Range.End - Range.Min) / (Range.Max - Range.Min) * size;
    var rectStart = new Rect(0, 0, start, bounds.Height);
    var rectSelected = new Rect(start, 0, end - start, bounds.Height);
    var rectEnd = new Rect(end, 0, size - end, bounds.Height);

    _startArea.Arrange(rectStart);
    _selectedArea.Arrange(rectSelected);
    _endArea.Arrange(rectEnd);
    _startThumb.Arrange(rectStart);
    _endThumb.Arrange(rectEnd);

    return arrangeSize;
  }

  private void OnSliderPreviewMouseDown(object sender, MouseButtonEventArgs e) {
    if (Range == null || _startThumb.IsMouseOver || _endThumb.IsMouseOver) return;

    var point = e.GetPosition(_sliderContainer);
    if (e.ChangedButton == MouseButton.Left)
      MoveThumbTo(point.X, true);
    else if (e.ChangedButton == MouseButton.Right)
      MoveThumbTo(point.X, false);

    e.Handled = true;
  }

  private void MoveThumbTo(double position, bool start) {
    double size = _sliderContainer.ActualWidth;
    if (double.IsNaN(size) || !(size > 0)) return;
    var value = Math.Min(Range.Max, Range.Min + (position / size) * (Range.Max - Range.Min)).RoundTo(TickFrequency);
        
    if (start) Range.Start = Math.Min(Range.End, value);
    else Range.End = Math.Max(Range.Start, value);

    ArrangeOverride(RenderSize);
    Range.RaiseChangedEvent();
  }

  private static void OnDragCompletedEvent(object sender, DragCompletedEventArgs e) =>
    (sender as RangeSlider)?.Range?.RaiseChangedEvent();

  private static void OnThumbDragDelta(object sender, DragDeltaEventArgs e) =>
    (sender as RangeSlider)?.OnThumbDragDelta(e);

  private void OnThumbDragDelta(DragDeltaEventArgs e) {
    if (Range == null || e.OriginalSource is not Thumb thumb || _sliderContainer == null) return;
    double change = e.HorizontalChange / _sliderContainer.ActualWidth * (Range.Max - Range.Min);

    if (ReferenceEquals(thumb, _startThumb))
      Range.Start = Math.Max(Range.Min, Math.Min(Range.End, Range.Start + change)).RoundTo(TickFrequency);
    else if (ReferenceEquals(thumb, _endThumb))
      Range.End = Math.Min(Range.Max, Math.Max(Range.Start, Range.End + change)).RoundTo(TickFrequency);

    ArrangeOverride(RenderSize);
  }
  
  private static void OnRangePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    if (d is not RangeSlider self) return;
    if (e.OldValue is SelectionRange oldRange) oldRange.PropertyChanged -= self.OnAnyRangePropertyChanged;
    if (e.NewValue is SelectionRange newRange) newRange.PropertyChanged += self.OnAnyRangePropertyChanged;
  }

  private void OnAnyRangePropertyChanged(object sender, PropertyChangedEventArgs e) =>
    ArrangeOverride(RenderSize);
}