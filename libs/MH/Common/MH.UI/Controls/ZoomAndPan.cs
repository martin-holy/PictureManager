using MH.Utils;
using MH.Utils.BaseClasses;
using System;

namespace MH.UI.Controls;

public interface IZoomAndPanHost {
  public double Width { get; }
  public double Height { get; }

  public void StartAnimation(double toValue, double duration, bool horizontal, Action onCompleted);
  public void StopAnimation();
}

public class ZoomAndPan : ObservableObject {
  private double _startX;
  private double _startY;
  private double _originX;
  private double _originY;
  private bool _isZoomed;

  private double _scaleX;
  private double _scaleY;
  private double _transformX;
  private double _transformY;
  private double _contentWidth;
  private double _contentHeight;
  private bool _isAnimationOn;
  private bool _expandToFill;
  private bool _shrinkToFill = true;

  public IZoomAndPanHost? Host { get; set; }
  public double ScaleX { get => _scaleX; set { _scaleX = value; OnPropertyChanged(); } }
  public double ScaleY { get => _scaleY; set { _scaleY = value; OnPropertyChanged(); } }
  public double TransformX { get => _transformX; set { _transformX = value; OnPropertyChanged(); } }
  public double TransformY { get => _transformY; set { _transformY = value; OnPropertyChanged(); } }
  public double ContentWidth { get => _contentWidth; set { _contentWidth = value; OnPropertyChanged(); } }
  public double ContentHeight { get => _contentHeight; set { _contentHeight = value; OnPropertyChanged(); } }
  public bool IsAnimationOn { get => _isAnimationOn; set { _isAnimationOn = value; OnPropertyChanged(); } }
  public bool ExpandToFill { get => _expandToFill; set { _expandToFill = value; OnPropertyChanged(); } }
  public bool ShrinkToFill { get => _shrinkToFill; set { _shrinkToFill = value; OnPropertyChanged(); } }
  public double ActualZoom => _scaleX * 100;

  public event EventHandler AnimationEndedEvent = delegate { };

  private void _raiseAnimationEnded() => AnimationEndedEvent(this, EventArgs.Empty);

  private void _setScale(double scale, double relativeX, double relativeY) {
    var absoluteX = (relativeX * _scaleX) + _transformX;
    var absoluteY = (relativeY * _scaleY) + _transformY;
    ScaleX = scale;
    ScaleY = scale;
    TransformX = absoluteX - (relativeX * _scaleX);
    TransformY = absoluteY - (relativeY * _scaleY);
    OnPropertyChanged(nameof(ActualZoom));
  }

  public void ScaleToFit() {
    if (Host == null) return;
    var scale = _getFitScale(Host.Width, Host.Height);
    ScaleX = scale;
    ScaleY = scale;
    TransformX = (Host.Width - (_contentWidth * scale)) / 2;
    TransformY = (Host.Height - (_contentHeight * scale)) / 2;
    _isZoomed = false;
    OnPropertyChanged(nameof(ActualZoom));
  }

  public void ScaleToFitContent(double width, double height) {
    ContentWidth = width;
    ContentHeight = height;
    ScaleToFit();
  }

  private double _getFitScale(double hostW, double hostH) {
    double scaleW = hostW / _contentWidth;
    double scaleH = hostH / _contentHeight;
    double scale = 1.0;

    if (_shrinkToFill && (_contentWidth > hostW || _contentHeight > hostH)) {
      scale = Math.Min(scaleW, scaleH);
    } else if (_expandToFill && (_contentWidth < hostW || _contentHeight < hostH)) {
      scale = Math.Min(scaleW, scaleH);
    }

    return scale;
  }

  public void StartAnimation(int minDuration) {
    if (Host == null) return;
    var horizontal = Host.Height / _contentHeight * _contentWidth > Host.Width;
    var scale = horizontal
      ? Host.Height / _contentHeight
      : Host.Width / _contentWidth;

    if (scale > 1) scale = 1;

    var toValue = horizontal
      ? ((_contentWidth * scale) - Host.Width) * -1
      : ((_contentHeight * scale) - Host.Height) * -1;

    _setScale(scale, _contentWidth / 2, _contentHeight / 2);

    var duration = toValue * 10 * -1 > minDuration
      ? toValue * 10 * -1
      : minDuration;

    IsAnimationOn = true;
    Host.StartAnimation(toValue, duration, horizontal, () => _onAnimationCompleted(toValue, horizontal));
  }

  private void _onAnimationCompleted(double toValue, bool horizontal) {
    if (!_isAnimationOn) return;

    if (horizontal)
      TransformX = toValue;
    else
      TransformY = toValue;

    IsAnimationOn = false;
    Host?.StopAnimation();
    _raiseAnimationEnded();
  }

  public void StopAnimation() {
    if (!_isAnimationOn) return;
    IsAnimationOn = false;
    Host?.StopAnimation();
  }

  public void OnHostSizeChanged() =>
    ScaleToFit();

  public void OnHostMouseMove(double hostPosX, double hostPosY) {
    TransformX = _originX - (_startX - hostPosX);
    TransformY = _originY - (_startY - hostPosY);
  }

  public void OnContentMouseDown(double hostPosX, double hostPosY, double contentPosX, double contentPosY) {
    if (!_isZoomed)
      _setScale(1, contentPosX, contentPosY);

    _startX = hostPosX;
    _startY = hostPosY;
    _originX = _transformX;
    _originY = _transformY;
  }

  public void OnContentMouseUp() {
    if (!_isZoomed)
      ScaleToFit();
  }

  public void OnContentMouseWheel(int delta, double contentPosX, double contentPosY) {
    if (!Keyboard.IsCtrlOn() || (!(delta > 0) && (_scaleX < .2 || _scaleY < .2))) return;

    _isZoomed = true;
    var scale = _scaleX + (delta > 0 ? .1 : -.1);
    _setScale(scale, contentPosX, contentPosY);
  }

  public bool IsContentPanoramic() =>
    Host != null && ContentWidth / (ContentHeight / Host.Height) > Host.Width;
}