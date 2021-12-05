using PictureManager.Dialogs;
using PictureManager.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class SegmentRectsControl : UserControl {
    public static readonly DependencyProperty MediaItemProperty = DependencyProperty.Register(
      nameof(MediaItem), typeof(MediaItemM), typeof(SegmentRectsControl), new PropertyMetadata(new PropertyChangedCallback(OnMediaItemChanged)));
    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
      nameof(Zoom), typeof(double), typeof(SegmentRectsControl), new PropertyMetadata(new PropertyChangedCallback(OnZoomChanged)));
    public static readonly DependencyProperty IsEditOnProperty = DependencyProperty.Register(
      nameof(IsEditOn), typeof(bool), typeof(SegmentRectsControl));

    public MediaItemM MediaItem { get => (MediaItemM)GetValue(MediaItemProperty); set => SetValue(MediaItemProperty, value); }
    public double Zoom { get => (double)GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
    public bool IsEditOn { get => (bool)GetValue(IsEditOnProperty); set => SetValue(IsEditOnProperty, value); }

    private SegmentRect _current;
    private double _scale;
    private bool _isEditModeMove;
    private bool _isCurrentModified;

    public ObservableCollection<SegmentRect> MediaItemSegmentsRects { get; } = new();

    public SegmentRectsControl() {
      InitializeComponent();
    }

    private static void OnMediaItemChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
      ((SegmentRectsControl)o).ReloadMediaItemSegmentRects();

    private static void OnZoomChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
      ((SegmentRectsControl)o).UpdateScale();

    public void ReloadMediaItemSegmentRects() {
      _current = null;
      MediaItemSegmentsRects.Clear();
      if (MediaItem?.Segments == null) return;
      _scale = Zoom / 100;

      App.Core.SegmentsM.DeselectAll();

      foreach (var segment in MediaItem.Segments)
        MediaItemSegmentsRects.Add(new SegmentRect(segment, _scale));
    }

    public void UpdateScale() {
      _scale = Zoom / 100;
      foreach (var sr in MediaItemSegmentsRects)
        sr.Scale = _scale;
    }

    private void CreateNew(Point mpos) {
      var segment = App.Core.SegmentsM.AddNewSegment((int)(mpos.X / _scale), (int)(mpos.Y / _scale), 0, MediaItem);
      _isEditModeMove = false;
      _isCurrentModified = true;
      _current = new SegmentRect(segment, _scale);
      MediaItemSegmentsRects.Add(_current);
    }

    private void StartEdit(Point mpos) {
      _isCurrentModified = true;
      IsEditOn = true;

      if (_isEditModeMove) {
        _current.X = (int)mpos.X;
        _current.Y = (int)mpos.Y;
      }
      else {
        var x = _current.X + _current.Radius;
        var y = _current.Y + _current.Radius;
        _current.Radius = (int)Math.Max(Math.Abs(x - mpos.X), Math.Abs(y - mpos.Y));
      }
    }

    private void EndEdit() {
      if (_current == null) {
        App.Core.SegmentsM.DeselectAll();
        return;
      }

      if (_isCurrentModified) {
        App.Core.SegmentsM.DataAdapter.IsModified = true;
        _ = _current.Segment.SetPictureAsync(App.Core.SegmentsM.SegmentSize, true);
        _isCurrentModified = false;
        IsEditOn = false;
      }

      _current = null;
    }

    private void SegmentRect_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      if (e.Source is FrameworkElement fe && (fe.Name.Equals("MovePoint") || fe.Name.Equals("ResizeBorder"))) {
        _isEditModeMove = fe.Name.Equals("MovePoint");
        _current = (SegmentRect)fe.DataContext;
        App.Core.SegmentsM.DeselectAll();
        App.Core.SegmentsM.SetSelected(_current.Segment, true);
      }
    }

    public void SegmentRectsControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0 || e.RightButton == MouseButtonState.Pressed)
        CreateNew(e.GetPosition(this));
    }

    public void SegmentRectsControl_PreviewMouseMove(object sender, MouseEventArgs e) {
      if (e.RightButton != MouseButtonState.Pressed && e.LeftButton != MouseButtonState.Pressed) {
        if (_current != null)
          EndEdit();
        return;
      }

      if (_current != null) {
        e.Handled = true;
        StartEdit(e.GetPosition(this));
      }
    }

    public void SegmentRectsControl_PreviewMouseUp(object sender, MouseButtonEventArgs e) => EndEdit();

    private void BtnDelete_Click(object sender, RoutedEventArgs e) {
      if (((FrameworkElement)sender).DataContext is not SegmentRect segmentRect) return;
      if (!MessageDialog.Show("Delete Segment", "Do you really want to delete this segment?", true)) return;
      App.Core.SegmentsM.Delete(segmentRect.Segment);
      _ = MediaItemSegmentsRects.Remove(segmentRect);
    }
  }

  public class SegmentRect : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private double _scale;

    public int X {
      get => (int)((Segment.RotateTransformGetX(Segment.X) - Segment.Radius) * Scale);
      set {
        Segment.RotateTransformSetX((int)(value / Scale));
        OnPropertyChanged();
      }
    }

    public int Y {
      get => (int)((Segment.RotateTransformGetY(Segment.Y) - Segment.Radius) * Scale);
      set {
        Segment.RotateTransformSetY((int)(value / Scale));
        OnPropertyChanged();
      }
    }

    public int Size => (int)(Segment.Radius * 2 * Scale);

    public int Radius {
      get => (int)(Segment.Radius * Scale);
      set {
        Segment.Radius = (int)(value / Scale);
        OnPropertyChanged();
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Size));
      }
    }

    public double Scale {
      get => _scale;
      set {
        _scale = value;
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Size));
        OnPropertyChanged(nameof(Radius));
      }
    }

    public SegmentM Segment { get; set; }

    public SegmentRect(SegmentM segment, double scale) {
      Segment = segment;
      Scale = scale;
    }
  }
}
