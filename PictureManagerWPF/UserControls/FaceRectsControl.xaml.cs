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

    public MediaItem MediaItem { get => (MediaItem)GetValue(MediaItemProperty); set => SetValue(MediaItemProperty, value); }
    public double Zoom { get => (double)GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }

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

      foreach (var face in MediaItem.Faces)
        MediaItemFaceRects.Add(new FaceRect(face, _scale));
    }

    public void UpdateScale() {
      _scale = Zoom / 100;
      foreach (var fr in MediaItemFaceRects)
        fr.Scale = _scale;
    }

    private void FaceRect_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      if (e.OriginalSource is FrameworkElement fe && (fe.Name.Equals("MoveEllipse") || fe.Name.Equals("ResizeBorder"))) {
        _isEditModeMove = fe.Name.Equals("MoveEllipse");
        _current = (FaceRect)fe.DataContext;
        App.Core.Faces.DeselectAll();
        App.Core.Faces.SetSelected(_current.Face, true);
      }
    }

    public async void FaceRectsControl_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
      if (_current == null) return;
      if (!_isCurrentModified) {
        _current = null;
        return;
      }

      if (_current.Size == 1)
        _current.IsSquare = false;

      App.Db.SetModified<Faces>();
      await _current.Face.SetPictureAsync(App.Core.Faces.FaceSize, true);
      _isCurrentModified = false;
      _current = null;
    }

    public async void FaceRectsControl_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) {
        var mpos = e.GetPosition(this);
        var face = await App.Core.Faces.AddNewFace((int)(mpos.X / _scale), (int)(mpos.Y / _scale), 1, MediaItem);
        _isEditModeMove = false;
        _isCurrentModified = true;
        _current = new FaceRect(face, _scale);
        MediaItemFaceRects.Add(_current);
      }
    }

    public void FaceRectsControl_PreviewMouseMove(object sender, MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed || _current == null) return;
      e.Handled = true;
      _isCurrentModified = true;

      var mpos = e.GetPosition(this);
      var half = _current.Size / 2.0;
      var x = _current.Size == 0 ? _current.X : _current.X + half;
      var y = _current.Size == 0 ? _current.Y : _current.Y + half;
      var diff = Math.Max(Math.Abs(x - mpos.X), Math.Abs(y - mpos.Y));

      if (_isEditModeMove) {
        _current.X = (int)mpos.X;
        _current.Y = (int)mpos.Y;
      }
      else {
        _current.Size = (int)diff * 2;
      }
    }

    private void BtnShape_Click(object sender, RoutedEventArgs e) {
      if (((FrameworkElement)sender).DataContext is not FaceRect faceRect) return;
      faceRect.IsSquare = !faceRect.IsSquare;
      App.Db.SetModified<Faces>();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e) {
      if (((FrameworkElement)sender).DataContext is not FaceRect faceRect) return;
      if (!MessageDialog.Show("Delete Face", $"Do you realy want to delete this face?", true)) return;
      App.Core.Faces.Delete(faceRect.Face);
      _ = MediaItemFaceRects.Remove(faceRect);
    }
  }

  public class FaceRect : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private const int _minSquareSize = 100;
    private double _scale;

    public bool IsSquare {
      get => Face.Size != 0;
      set {
        Size = value ? _minSquareSize : 0;
        OnPropertyChanged();
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Size));
        OnPropertyChanged(nameof(BorderSize));
      }
    }

    public int X {
      get => (int)((Face.X - (Face.Size / 2.0)) * Scale) - (IsSquare ? 0 : _minSquareSize / 2);
      set {
        Face.X = (int)(value / Scale);
        OnPropertyChanged();
      }
    }

    public int Y {
      get => (int)((Face.Y - (Face.Size / 2.0)) * Scale) - (IsSquare ? 0 : _minSquareSize / 2);
      set {
        Face.Y = (int)(value / Scale);
        OnPropertyChanged();
      }
    }

    public int BorderSize => Size == 0 ? _minSquareSize : Size;

    public int Size {
      get => (int)(Face.Size * Scale);
      set {
        Face.Size = (int)(value / Scale);
        OnPropertyChanged();
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(BorderSize));
      }
    }

    public double Scale {
      get => _scale;
      set {
        _scale = value;
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Size));
        OnPropertyChanged(nameof(BorderSize));
      }
    }

    public Face Face { get; set; }

    public FaceRect(Face face, double scale) {
      Face = face;
      Scale = scale;
    }
  }
}
