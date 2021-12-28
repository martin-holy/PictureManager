using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;

namespace PictureManager.Views {
  public partial class ZoomImageBoxV : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged(this, new(name));

    private Point _origin;
    private Point _start;
    private bool _isZoomed;

    private double _scaleX;
    private double _scaleY;
    private double _transformX;
    private double _transformY;
    private double _imageWidth;
    private double _imageHeight;
    private BitmapImage _imageSource;

    public double ScaleX {
      get => _scaleX;
      set {
        _scaleX = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(ActualZoom));
        ScaleChangedEventHandler(this, EventArgs.Empty);
      }
    }
    public double ScaleY { get => _scaleY; set { _scaleY = value; OnPropertyChanged(); } }
    public double TransformX { get => _transformX; set { _transformX = value; OnPropertyChanged(); } }
    public double TransformY { get => _transformY; set { _transformY = value; OnPropertyChanged(); } }
    public double ImageWidth { get => _imageWidth; set { _imageWidth = value; OnPropertyChanged(); } }
    public double ImageHeight { get => _imageHeight; set { _imageHeight = value; OnPropertyChanged(); } }
    public BitmapImage ImageSource { get => _imageSource; set { _imageSource = value; OnPropertyChanged(); } }
    public bool IsAnimationOn { get; set; }
    public double ActualZoom => ScaleX * 100;

    public event EventHandler ScaleChangedEventHandler = delegate { };

    public ZoomImageBoxV() {
      InitializeComponent();
    }

    public void SetSource(MediaItemM mi) {
      if (mi == null) {
        ImageSource = null;
        return;
      }

      var src = new BitmapImage();
      src.BeginInit();
      src.UriSource = new(mi.FilePath);
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
      src.Rotation = Imaging.MediaOrientation2Rotation((MediaOrientation)mi.Orientation);
      src.EndInit();

      ImageWidth = src.PixelWidth;
      ImageHeight = src.PixelHeight;
      ImageSource = src;
      ScaleToFit();
    }

    public void Play(int minDuration, Action callback) {
      var horizontal = ActualHeight / ImageHeight * ImageWidth > ActualWidth;
      var scale = horizontal
        ? ActualHeight / ImageHeight
        : ActualWidth / ImageWidth;

      if (scale > 1) scale = 1;

      var toValue = horizontal
        ? (ImageWidth * scale - ActualWidth) * -1
        : (ImageHeight * scale - ActualHeight) * -1;

      SetScale(scale, new(ImageWidth / 2, ImageHeight / 2));

      var duration = toValue * 10 * -1 > minDuration
        ? toValue * 10 * -1
        : minDuration;
      var animation = new DoubleAnimation(0, toValue, TimeSpan.FromMilliseconds(duration), FillBehavior.Stop);
      
      animation.Completed += (_, _) => {
        if (!IsAnimationOn) return;

        if (horizontal)
          TransformX = toValue;
        else
          TransformY = toValue;

        IsAnimationOn = false;
        callback();
      };

      IsAnimationOn = true;
      ImageTransform.BeginAnimation(horizontal
        ? TranslateTransform.XProperty
        : TranslateTransform.YProperty, animation);
    }

    public void Stop() {
      ImageTransform.BeginAnimation(TranslateTransform.XProperty, null);
      IsAnimationOn = false;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
      ScaleToFit();
    }

    private void OnMouseMove(object sender, MouseEventArgs e) {
      if (!Image.IsMouseCaptured) return;
      var v = _start - e.GetPosition(Canvas);
      TransformX = _origin.X - v.X;
      TransformY = _origin.Y - v.Y;
    }

    private void Image_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (!_isZoomed)
        SetScale(1, e.GetPosition(Image));

      _start = e.GetPosition(Canvas);
      _origin = new(TransformX, TransformY);
        
      Canvas.Cursor = Cursors.Hand;
      Image.CaptureMouse();
    }

    private void Image_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      Canvas.Cursor = Cursors.Arrow;
      Image.ReleaseMouseCapture();

      if (!_isZoomed)
        ScaleToFit();
    }

    private void Image_OnMouseWheel(object sender, MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) == 0
          || !(e.Delta > 0) && (ScaleX < .2 || ScaleY < .2)) return;

      _isZoomed = true;
      var scale = ScaleX + (e.Delta > 0 ? .1 : -.1);
      SetScale(scale, e.GetPosition(Image));
    }

    private void SetScale(double scale, Point relative) {
      var absoluteX = relative.X * ScaleX + TransformX;
      var absoluteY = relative.Y * ScaleY + TransformY;

      ScaleX = scale;
      ScaleY = scale;

      TransformX = absoluteX - relative.X * ScaleX;
      TransformY = absoluteY - relative.Y * ScaleY;
    }

    private void ScaleToFit() {
      if (ImageSource == null) return;

      var scale = GetFitScale(ImageSource.PixelWidth, ImageSource.PixelHeight, ActualWidth, ActualHeight);
      ScaleX = scale;
      ScaleY = scale;
      TransformX = (ActualWidth - ImageSource.PixelWidth * scale) / 2;
      TransformY = (ActualHeight - ImageSource.PixelHeight * scale) / 2;
      _isZoomed = false;
    }

    private static double GetFitScale(double imageW, double imageH, double screenW, double screenH) {
      double iw = imageW;
      double ih = imageH;

      if (iw > screenW) {
        iw = screenW;
        ih = imageH / (imageW / screenW);

        if (ih > screenH) {
          ih = screenH;
          iw = imageW / (imageH / screenH);
        }
      }

      if (ih > screenH) {
        iw = imageW / (imageH / screenH);

        if (iw > screenW)
          iw = screenW;
      }

      return iw / imageW;
    }
  }
}
