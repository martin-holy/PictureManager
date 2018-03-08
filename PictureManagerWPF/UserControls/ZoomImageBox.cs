using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PictureManager.UserControls {
  public sealed class ZoomImageBox : Border {
    private Point _origin;
    private Point _start;
    private bool _isDecoded;
    private readonly ScaleTransform _scaleTransform;
    private readonly RotateTransform _rotateTransform;
    private readonly TranslateTransform _translateTransform;
    private string _filePath;
    private MediaOrientation _orientation;

    public ZoomImageBox() {
      _isDecoded = true;

      _scaleTransform = new ScaleTransform();
      _rotateTransform = new RotateTransform();
      _translateTransform = new TranslateTransform();
      var renderGroup = new TransformGroup();
      renderGroup.Children.Add(_scaleTransform);
      renderGroup.Children.Add(_translateTransform);
      var layoutGroup = new TransformGroup();
      layoutGroup.Children.Add(_rotateTransform);
      Image = new Image {
        LayoutTransform = layoutGroup,
        RenderTransform = renderGroup,
        RenderTransformOrigin = new Point(0, 0)
      };
      ImgBorder = new Border {Child = Image};
      Child = ImgBorder;

      MouseMove += OnMouseMove;
      MouseWheel += OnMouseWheel;
      MouseLeftButtonUp += OnMouseLeftButtonUp;
      MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    public Image Image;
    public Border ImgBorder;
    public string FilePath { get => _filePath; set { _filePath = value; SetSource(); } }
    public MediaOrientation Orientation {
      get => _orientation;
      set {
        _orientation = value;

        switch (value) {
          case MediaOrientation.Normal:
            _rotateTransform.Angle = 0;
            break;
          case MediaOrientation.FlipHorizontal:
            break;
          case MediaOrientation.Rotate180:
            _rotateTransform.Angle = 180;
            break;
          case MediaOrientation.FlipVertical:
            break;
          case MediaOrientation.Transpose:
            break;
          case MediaOrientation.Rotate270:
            _rotateTransform.Angle = 90;
            break;
          case MediaOrientation.Transverse:
            break;
          case MediaOrientation.Rotate90:
            _rotateTransform.Angle = 270;
            break;
        }
      }
    }

    public void SetSource() {
      Reset();
      var src = new BitmapImage();
      src.BeginInit();
      src.UriSource = new Uri(FilePath);
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
      if (_isDecoded)
        src.DecodePixelWidth = 1920;
      src.EndInit();
      Image.Source = src;
      GC.Collect();
    }

    private void Reset() {
      // reset zoom
      _scaleTransform.ScaleX = 1.0;
      _scaleTransform.ScaleY = 1.0;
      // reset pan
      _translateTransform.X = 0.0;
      _translateTransform.Y = 0.0;
    }

    private void OnMouseMove(object o, MouseEventArgs e) {
      if (!Image.IsMouseCaptured) return;
      var v = _start - e.GetPosition(this);
      _translateTransform.X = _origin.X - v.X;
      _translateTransform.Y = _origin.Y - v.Y;
    }

    private void OnMouseWheel(object o, MouseWheelEventArgs e) {
      if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) return;
      if (_isDecoded) {
        _isDecoded = false;
        SetSource();
      }

      var zoom = e.Delta > 0 ? .2 : -.2;
      if (!(e.Delta > 0) && (_scaleTransform.ScaleX < .4 || _scaleTransform.ScaleY < .4))
        return;


      /*var relativeX = relative.X;
      relative.X = relative.Y;
      relative.Y = relativeX;*/
      var bla = e.GetPosition(ImgBorder);
      var relative = e.GetPosition(Image);
      var abosuluteX = relative.X * _scaleTransform.ScaleX + _translateTransform.X;
      var abosuluteY = relative.Y * _scaleTransform.ScaleY + _translateTransform.Y;

      _scaleTransform.ScaleX += zoom;
      _scaleTransform.ScaleY += zoom;

      _translateTransform.X = abosuluteX - relative.X * _scaleTransform.ScaleX;
      _translateTransform.Y = abosuluteY - relative.Y * _scaleTransform.ScaleY;
    }

    private void OnMouseLeftButtonUp(object o, MouseButtonEventArgs e) {
      Image.ReleaseMouseCapture();
      Cursor = Cursors.Arrow;
    }

    private void OnMouseLeftButtonDown(object o, MouseButtonEventArgs e) {
      _start = e.GetPosition(this);
      _origin = new Point(_translateTransform.X, _translateTransform.Y);
      Cursor = Cursors.Hand;
      Image.CaptureMouse();
    }
  }
}
