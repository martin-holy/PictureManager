﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.CustomControls {
  public sealed class ZoomImageBox : Border, INotifyPropertyChanged {
    private Point _origin;
    private Point _start;
    private bool _isDecoded;
    private bool _isBigger;
    private bool _isZoomed;
    private double _zoomScale100;
    private double _zoomScale;
    private double _zoomActual;
    private readonly ScaleTransform _scaleTransform;
    private readonly TranslateTransform _translateTransform;
    private MediaItem _currentMediaItem;

    public Image Image;
    public event PropertyChangedEventHandler PropertyChanged;
    public bool IsAnimationOn;

    public double ZoomActual {
      get => _zoomActual;
      set {
        _zoomActual = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(ZoomActualFormatted));
        // TODO: find another way to do it
        App.Core.AppInfo.OnPropertyChanged(nameof(ZoomActualFormatted));
      }
    }

    public string ZoomActualFormatted => $"{_zoomActual:####}%";

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
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;

        if (_isDecoded)
          SetSource(false);

        if (!(e.Delta > 0) && (_scaleTransform.ScaleX < .4 || _scaleTransform.ScaleY < .4)) return;

        _zoomScale += e.Delta > 0 ? .2 : -.2;
        _isZoomed = true;
        SetScale(_zoomScale, e.GetPosition(Image));
      };

      //Event MouseLeftButtonUp
      MouseLeftButtonUp += (o, e) => {
        Image.ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
        if (_isZoomed || Image.Source == null) return;
        Reset();
        ZoomActual = _isBigger ? (Image.ActualWidth / ((BitmapImage) Image.Source).PixelWidth) * 100 : 100;
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
      var absoluteX = relative.X * _scaleTransform.ScaleX + _translateTransform.X;
      var absoluteY = relative.Y * _scaleTransform.ScaleY + _translateTransform.Y;

      _scaleTransform.ScaleX = zoom;
      _scaleTransform.ScaleY = zoom;

      _translateTransform.X = absoluteX - relative.X * _scaleTransform.ScaleX;
      _translateTransform.Y = absoluteY - relative.Y * _scaleTransform.ScaleY;

      ZoomActual = ((Image.ActualWidth * zoom) / ((BitmapImage) Image.Source).PixelWidth) * 100;
    }

    public void SetSource(MediaItem currentMediaItem, bool decoded = false) {
      _currentMediaItem = currentMediaItem;
      SetSource(decoded);
    }

    private void SetSource(bool decoded) {
      _isDecoded = decoded;
      Reset();
      if (_currentMediaItem == null) {
        Image.Source = null;
        return;
      }

      var rotated = _currentMediaItem.Orientation == (int) MediaOrientation.Rotate90 || 
                    _currentMediaItem.Orientation == (int) MediaOrientation.Rotate270;
      var imgWidth = rotated ? _currentMediaItem.Height : _currentMediaItem.Width;
      var imgHeight = rotated ? _currentMediaItem.Width : _currentMediaItem.Height;
      _isBigger = ActualWidth < imgWidth || ActualHeight < imgHeight;

      var src = new BitmapImage();
      src.BeginInit();
      src.UriSource = _currentMediaItem.FilePathUri;
      src.CacheOption = BitmapCacheOption.OnLoad;
      src.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;

      //bad quality with decoding
      /*if (_isBigger && decoded) {
        if (imgWidth > imgHeight)
          if (rotated)
            src.DecodePixelWidth = (int) ActualHeight;
          else
            src.DecodePixelHeight = (int) ActualHeight;
        else
          if (rotated)
            src.DecodePixelHeight = (int) ActualWidth;
          else
            src.DecodePixelWidth = (int) ActualWidth;
      }*/

      switch (_currentMediaItem.Orientation) {
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

      src.EndInit();

      Image.Width = _isBigger ? ActualWidth : src.PixelWidth;
      Image.Height = _isBigger ? ActualHeight : src.PixelHeight;
      Image.Source = src;
      UpdateLayout();
      _isZoomed = false;
      _zoomScale100 = _isBigger ? src.PixelWidth / Image.ActualWidth : 1;
      ZoomActual = _isBigger ? (Image.ActualWidth / src.PixelWidth) * 100 : 100;
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

    public void Play(int minDuration, Action callback) {
      var pano = Image.ActualHeight < ActualHeight;
      var zoomScale = pano
        ? ActualHeight / (Image.ActualHeight / 100) / 100
        : ActualWidth / (Image.ActualWidth / 100) / 100;
      if (zoomScale > _zoomScale100) zoomScale = _zoomScale100;
      var toValue = pano
        ? ((Image.ActualWidth * zoomScale) - Image.ActualWidth) * -1
        : ((Image.ActualHeight * zoomScale) - Image.ActualHeight) * -1;
      SetScale(zoomScale, new Point(Image.ActualWidth / 2, Image.ActualHeight / 2));
      var duration = toValue * 10 * -1 > minDuration ? toValue * 10 * -1 : minDuration;
      var animation = new DoubleAnimation(0, toValue, TimeSpan.FromMilliseconds(duration), FillBehavior.Stop);
      animation.Completed += (o, e) => {
        if (!IsAnimationOn) return;
        if (pano)
          _translateTransform.X = toValue;
        else
          _translateTransform.Y = toValue;
        IsAnimationOn = false;
        callback();
      };
      IsAnimationOn = true;
      _translateTransform.BeginAnimation(pano ? TranslateTransform.XProperty : TranslateTransform.YProperty, animation);
    }

    public void Stop() {
      _translateTransform.BeginAnimation(TranslateTransform.XProperty, null);
      IsAnimationOn = false;
      Reset();
    }

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
