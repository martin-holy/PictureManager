using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.MediaItem.Image;
using PictureManager.Common.Features.MediaItem.Video;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Features.MediaItem;

public enum SlideshowState { On, Paused, Stopped }

public sealed class SlideshowVM : ObservableObject {
  private readonly ZoomAndPan _zoomAndPan;
  private bool _isTimerOn;
  private int _repeatCount;
  private readonly MediaViewerVM _mediaViewer;
  private MediaItemM? _current;

  private bool _playPanoramicImages = true;
  private int _interval = 3;
  private SlideshowState _state = SlideshowState.Stopped;

  public bool PlayPanoramicImages { get => _playPanoramicImages; set { _playPanoramicImages = value; OnPropertyChanged(); } }
  public int Interval { get => _interval; set { _interval = value; OnPropertyChanged(); } }
  public SlideshowState State { get => _state; set { _state = value; OnPropertyChanged(); } }

  public RelayCommand StartCommand { get; }
  public RelayCommand StopCommand { get; }

  public SlideshowVM(MediaViewerVM mediaViewer, ZoomAndPan zoomAndPan) {
    _zoomAndPan = zoomAndPan;
    _mediaViewer = mediaViewer;
    zoomAndPan.AnimationEndedEvent += _onZoomAndPanAnimationEnded;
    StartCommand = new(_slideshow, MH.UI.Res.IconPlay, "Start slideshow");
    StopCommand = new(_slideshow, MH.UI.Res.IconStop, "Stop slideshow");
  }

  public void OnCurrentChanged(MediaItemM current) {
    _current = current;
    if (_state != SlideshowState.Stopped)
      _next(true);
  }

  private void _onZoomAndPanAnimationEnded(object? sender, EventArgs e) {
    if (_state != SlideshowState.Paused) return;
    State = SlideshowState.On;
    _next(false);
  }

  public void OnPlayerMediaEnded(object? sender, EventArgs e) {
    if (_state != SlideshowState.Paused) return;

    if (_repeatCount == 0) {
      var rc = _interval / (((MediaPlayer)sender!).TimelineMaximum / 1000);
      _repeatCount = (int)rc;
      if (rc - _repeatCount > 0) _repeatCount++;
    }

    _repeatCount--;

    if (_repeatCount <= 0) {
      State = SlideshowState.On;
      _next(false);
    }
  }

  public bool Stop() {
    if (_state == SlideshowState.Stopped) return false;
    State = SlideshowState.Stopped;
    _zoomAndPan.StopAnimation();
    return true;
  }

  private void _next(bool delay) {
    if (!delay) {
      if (!_mediaViewer.Next()) Stop();
      return;
    }

    switch (_current) {
      case ImageM img:
        if (_playPanoramicImages && img.IsPanoramic()) {
          State = SlideshowState.Paused;
          _zoomAndPan.StartAnimation(_interval * 1000);
        }
        else if (!_isTimerOn) {
          _isTimerOn = true;

          Task.Run(() => {
            Thread.Sleep(_interval * 1000);
            Tasks.RunOnUiThread(() => {
              _isTimerOn = false;

              if (_state != SlideshowState.Stopped)
                _next(false);
            });
          });
        }
        break;
      case VideoM:
        _repeatCount = 0;
        State = SlideshowState.Paused;
        break;
    }
  }

  private void _slideshow() {
    if (Stop()) return;
    State = SlideshowState.On;
    _next(true);
  }
}