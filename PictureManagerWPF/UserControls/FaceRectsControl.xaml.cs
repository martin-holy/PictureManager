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
  public partial class FaceRectsControl : UserControl {
    public static readonly DependencyProperty MediaItemProperty = DependencyProperty.Register(
      nameof(MediaItem), typeof(MediaItem), typeof(FaceRectsControl), new PropertyMetadata(new PropertyChangedCallback(OnMediaItemChanged)));
    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
      nameof(Zoom), typeof(double), typeof(FaceRectsControl), new PropertyMetadata(new PropertyChangedCallback(OnZoomChanged)));
    public static readonly DependencyProperty IsEditOnProperty = DependencyProperty.Register(
      nameof(IsEditOn), typeof(bool), typeof(FaceRectsControl));

    public MediaItem MediaItem { get => (MediaItem)GetValue(MediaItemProperty); set => SetValue(MediaItemProperty, value); }
    public double Zoom { get => (double)GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
    public bool IsEditOn { get => (bool)GetValue(IsEditOnProperty); set => SetValue(IsEditOnProperty, value); }

    private FaceRect _current;
    private double _scale;
    private bool _isEditModeMove;
    private bool _isCurrentModified;

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
      _scale = Zoom / 100;

      App.Core.Faces.DeselectAll();

      foreach (var face in MediaItem.Faces)
        MediaItemFaceRects.Add(new FaceRect(face, _scale));
    }

    public void UpdateScale() {
      _scale = Zoom / 100;
      foreach (var fr in MediaItemFaceRects)
        fr.Scale = _scale;
    }

    private void CreateNew(Point mpos) {
      var face = App.Core.Faces.AddNewFace((int)(mpos.X / _scale), (int)(mpos.Y / _scale), 0, MediaItem);
      _isEditModeMove = false;
      _isCurrentModified = true;
      _current = new FaceRect(face, _scale);
      MediaItemFaceRects.Add(_current);
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
      if (_current != null && _isCurrentModified) {
        App.Db.SetModified<Faces>();
        _ = _current.Face.SetPictureAsync(App.Core.Faces.FaceSize, true);
        _isCurrentModified = false;
      }

      IsEditOn = false;
      _current = null;
    }

    private void FaceRect_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      if (e.OriginalSource is FrameworkElement fe && (fe.Name.Equals("MoveEllipse") || fe.Name.Equals("ResizeBorder"))) {
        _isEditModeMove = fe.Name.Equals("MoveEllipse");
        _current = (FaceRect)fe.DataContext;
        App.Core.Faces.DeselectAll();
        App.Core.Faces.SetSelected(_current.Face, true);
      }
    }

    public void FaceRectsControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0 || e.RightButton == MouseButtonState.Pressed)
        CreateNew(e.GetPosition(this));
    }

    public void FaceRectsControl_PreviewMouseMove(object sender, MouseEventArgs e) {
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

    public void FaceRectsControl_PreviewMouseUp(object sender, MouseButtonEventArgs e) => EndEdit();

    private void BtnDelete_Click(object sender, RoutedEventArgs e) {
      if (((FrameworkElement)sender).DataContext is not FaceRect faceRect) return;
      if (!MessageDialog.Show("Delete Face", "Do you really want to delete this face?", true)) return;
      App.Core.Faces.Delete(faceRect.Face);
      _ = MediaItemFaceRects.Remove(faceRect);
    }
  }

  public class FaceRect : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private double _scale;

    public int X {
      get => (int)((Face.RotateTransformGetX(Face.X) - Face.Radius) * Scale);
      set {
        Face.RotateTransformSetX((int)(value / Scale));
        OnPropertyChanged();
      }
    }

    public int Y {
      get => (int)((Face.RotateTransformGetY(Face.Y) - Face.Radius) * Scale);
      set {
        Face.RotateTransformSetY((int)(value / Scale));
        OnPropertyChanged();
      }
    }

    public int Size => (int)(Face.Radius * 2 * Scale);

    public int Radius {
      get => (int)(Face.Radius * Scale);
      set {
        Face.Radius = (int)(value / Scale);
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

    public Face Face { get; set; }

    public FaceRect(Face face, double scale) {
      Face = face;
      Scale = scale;
    }
  }
}
