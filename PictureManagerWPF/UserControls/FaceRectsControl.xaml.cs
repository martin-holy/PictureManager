using PictureManager.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class FaceRectsControl : UserControl {
    public static readonly DependencyProperty MediaItemProperty = DependencyProperty.Register(
      nameof(MediaItem), typeof(MediaItem), typeof(FaceRectsControl), new PropertyMetadata(new PropertyChangedCallback(OnMediaItemChanged)));
    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
      nameof(Zoom), typeof(double), typeof(FaceRectsControl), new PropertyMetadata(new PropertyChangedCallback(OnZoomChanged)));
    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
      nameof(Scale), typeof(double), typeof(FaceRectsControl));

    public MediaItem MediaItem { get => (MediaItem)GetValue(MediaItemProperty); set => SetValue(MediaItemProperty, value); }
    public double Zoom { get => (double)GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
    public double Scale { get => (double)GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }

    private FaceRect _current;

    public ObservableCollection<FaceRect> MediaItemFaceRects { get; } = new();

    public FaceRectsControl() {
      InitializeComponent();
    }

    private static void OnMediaItemChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
      ((FaceRectsControl)o).ReloadMediaItemFaceRects();

    private static void OnZoomChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) =>
      ((FaceRectsControl)o).UpdateScale();

    public void ReloadMediaItemFaceRects() {
      _current = null;
      MediaItemFaceRects.Clear();
      if (MediaItem?.Faces == null) return;

      var scale = Zoom / Scale / 100;

      foreach (var face in MediaItem.Faces)
        MediaItemFaceRects.Add(new FaceRect(face, scale));
    }

    public void UpdateScale() {
      foreach (var fr in MediaItemFaceRects)
        fr.Scale = Zoom / Scale / 100;
    }

    private void Rectangle_PreviewMouseDown(object sender, MouseButtonEventArgs e) =>
      _current = (FaceRect)((FrameworkElement)sender).DataContext;

    public void FaceRectsControl_PreviewMouseUp(object sender, MouseButtonEventArgs e) => _current = null;

    public void FaceRectsControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) {
        var mpos = e.GetPosition(this);
        var scale = Zoom / Scale / 100;
        var face = new Face(0, 0, new Int32Rect((int)(mpos.X / scale), (int)(mpos.Y / scale), 0, 0));
        _current = new FaceRect(face, scale);
        MediaItemFaceRects.Add(_current);
      }
    }

    public void FaceRectsControl_PreviewMouseMove(object sender, MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed || _current == null) return;
      e.Handled = true;

      var mpos = e.GetPosition(this);
      var half = _current.Width / 2.0;
      var rpos = new Point(_current.X + half, _current.Y + half);
      var diff = Math.Max(Math.Abs(rpos.X - mpos.X), Math.Abs(rpos.Y - mpos.Y));

      if (diff > half - 5) { // resize
        var size = (int)(diff * 2);
        var offset = (_current.Width - size) / 2;

        _current.X += offset;
        _current.Y += offset;
        _current.Width = size;
        _current.Height = size;
      }
      else { // move
        _current.X += (int)(mpos.X - rpos.X);
        _current.Y += (int)(mpos.Y - rpos.Y);
      }
    }
  }

  public class FaceRect : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private int _x;
    private int _y;
    private int _width;
    private int _height;
    private double _scale;

    public int X { get => _x; set { _x = value; OnPropertyChanged(); } }
    public int Y { get => _y; set { _y = value; OnPropertyChanged(); } }
    public int Width { get => _width; set { _width = value; OnPropertyChanged(); } }
    public int Height { get => _height; set { _height = value; OnPropertyChanged(); } }
    public Face Face { get; set; }
    public double Scale { get => _scale; set { _scale = value; UpdateRect(); } }

    public FaceRect(Face face, double scale) {
      Face = face;
      Scale = scale;
    }

    private void UpdateRect() {
      var fb = Face.FaceBox;
      X = (int)(fb.X * Scale);
      Y = (int)(fb.Y * Scale);
      Width = (int)(fb.Width * Scale);
      Height = (int)(fb.Height * Scale);
    }
  }
}
