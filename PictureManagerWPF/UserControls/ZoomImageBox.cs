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
    private bool _isBigger;
    private bool _isZoomed;
    private double _zoomScale100;
    private double _zoomScale;
    private readonly ScaleTransform _scaleTransform;
    private readonly TranslateTransform _translateTransform;
    private ViewModel.BaseMediaItem _currentMediaItem;

    public Image Image;

    public ZoomImageBox() {
      _scaleTransform = new ScaleTransform();
      _translateTransform = new TranslateTransform();

      var renderGroup = new TransformGroup();
      renderGroup.Children.Add(_scaleTransform);
      renderGroup.Children.Add(_translateTransform);

      var layoutGroup = new TransformGroup();

      Image = new Image {
        LayoutTransform = layoutGroup,
        RenderTransform = renderGroup,
        RenderTransformOrigin = new Point(0, 0)
      };

      Child = Image;

      //Event MouseMove
      MouseMove += (o, e) => {
        if (!Image.IsMouseCaptured) return;
        var v = _start - e.GetPosition(this);
        _translateTransform.X = _origin.X - v.X;
        _translateTransform.Y = _origin.Y - v.Y;
      };

      //Event MouseWheel
      MouseWheel += (o, e) => {
        if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) return;

        if (_isDecoded) {
          _isDecoded = false;
          SetSource();
        }

        if (!(e.Delta > 0) && (_scaleTransform.ScaleX < .4 || _scaleTransform.ScaleY < .4)) return;

        _zoomScale += e.Delta > 0 ? .2 : -.2;
        _isZoomed = true;
        SetScale(_zoomScale, e.GetPosition(Image));
      };

      //Event MouseLeftButtonUp
      MouseLeftButtonUp += (o, e) => {
        Image.ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
        if (!_isZoomed) Reset();
      };

      //Event MouseLeftButtonDown
      MouseLeftButtonDown += (o, e) => {
        if (!_isZoomed) SetScale(_zoomScale100, e.GetPosition(Image));

        _start = e.GetPosition(this);
        _origin = new Point(_translateTransform.X, _translateTransform.Y);
        Cursor = Cursors.Hand;
        Image.CaptureMouse();
      };

      //Event SizeChanged
      SizeChanged += (o, e) => {
        if (!_isBigger) return;
        Image.Width = ActualWidth;
        Image.Height = ActualHeight;
      };
    }

    private void SetScale(double zoom, Point relative) {
      var abosuluteX = relative.X * _scaleTransform.ScaleX + _translateTransform.X;
      var abosuluteY = relative.Y * _scaleTransform.ScaleY + _translateTransform.Y;

      _scaleTransform.ScaleX = zoom;
      _scaleTransform.ScaleY = zoom;

      _translateTransform.X = abosuluteX - relative.X * _scaleTransform.ScaleX;
      _translateTransform.Y = abosuluteY - relative.Y * _scaleTransform.ScaleY;
    }

    public void SetSource(ViewModel.BaseMediaItem currentMediaItem) {
      _currentMediaItem = currentMediaItem;
      _isDecoded = true;
      SetSource();
    }

    private void SetSource() {
      Reset();
      if (_currentMediaItem == null) {
        Image.Source = null;
        return;
      }

      var rotated = _currentMediaItem.Data.Orientation == (int) MediaOrientation.Rotate90 || 
                    _currentMediaItem.Data.Orientation == (int) MediaOrientation.Rotate270;
      var imgWidth = rotated ? _currentMediaItem.Data.Height : _currentMediaItem.Data.Width;
      var imgHeight = rotated ? _currentMediaItem.Data.Width : _currentMediaItem.Data.Height;
      _isBigger = ActualWidth < imgWidth || ActualHeight < imgHeight;

      var src = new BitmapImage();
      src.BeginInit();
      src.UriSource = _currentMediaItem.FilePathUri;
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
      

      switch (_currentMediaItem.Data.Orientation) {
        case (int) MediaOrientation.Rotate90: {
          src.Rotation = Rotation.Rotate270;
          break;
        }
        case (int) MediaOrientation.Rotate180: {
          src.Rotation = Rotation.Rotate180;
          break;
        }
        case (int) MediaOrientation.Rotate270: {
          src.Rotation = Rotation.Rotate90;
          break;
        }
      }

      //bad quality with decoding
      _isDecoded = false;
      /*if (isBigger && _isDecoded) {
        if (decodeWidth)
          src.DecodePixelWidth = (int) ActualWidth;
        else
          src.DecodePixelHeight = (int) ActualHeight;
      }*/

      src.EndInit();

      Image.Stretch = Stretch.Uniform;
      Image.Width = _isBigger ? ActualWidth : src.PixelWidth;
      Image.Height = _isBigger ? ActualHeight : src.PixelHeight;
      Image.Source = src;
      UpdateLayout();
      _isZoomed = false;
      _zoomScale100 = _isBigger ? src.PixelWidth / (Image.ActualWidth / 100) / 100 : 1;
      GC.Collect();
    }

    private void Reset() {
      _zoomScale = 1.0;
      // reset zoom
      _scaleTransform.ScaleX = 1.0;
      _scaleTransform.ScaleY = 1.0;
      // reset pan
      _translateTransform.X = 0.0;
      _translateTransform.Y = 0.0;
    }
  }
}
