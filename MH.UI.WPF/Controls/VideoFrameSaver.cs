using MH.UI.HelperClasses;
using MH.UI.Interfaces;
using MH.UI.WPF.Extensions;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MH.UI.WPF.Controls;

/* Not visible parts of the VideoFrameSaver are filed with black color.
   But there is not problem with hiding VideoFrameSaver behind other controls. */

public class VideoFrameSaver : MediaElement, IVideoFrameSaver {
  private int _idxVideo;
  private int _idxFrame;
  private long _hash;
  private VfsVideo[] _videos;
  private VfsVideo _video;
  private VfsFrame _frame;
  private readonly DispatcherTimer _positionTimer;
  private readonly Stopwatch _timeOut = new();
  private readonly RotateTransform _rotateTransform = new();
  private Action _onFinishedAction;
  private Action<VfsFrame> _onSaveAction;

  static VideoFrameSaver() {
    DefaultStyleKeyProperty.OverrideMetadata(
      typeof(VideoFrameSaver),
      new FrameworkPropertyMetadata(typeof(VideoFrameSaver)));
  }

  public VideoFrameSaver() {
    LoadedBehavior = MediaState.Manual;
    IsMuted = true;
    Stretch = Stretch.Uniform;
    StretchDirection = StretchDirection.Both;
    ScrubbingEnabled = true;
    LayoutTransform = _rotateTransform;
    MediaOpened += delegate { OnMediaOpened(); };
    _positionTimer = new() { Interval = TimeSpan.FromMilliseconds(1) };
    _positionTimer.Tick += delegate { OnPositionTimerTick(); };
  }

  public void Save(VfsVideo[] videos, Action<VfsFrame> onSaveAction, Action onFinishedAction) {
    _videos = videos;
    _onSaveAction = onSaveAction;
    _onFinishedAction = onFinishedAction;
    _idxVideo = -1;
    MaxWidth = ((FrameworkElement)Parent).ActualWidth;
    MaxHeight = ((FrameworkElement)Parent).ActualHeight;
    NextVideo();
  }

  private void NextVideo() {
    if (_idxVideo + 1 > _videos.Length - 1) {
      Source = null;
      _frame = null;
      _video = null;
      _videos = null;
      _onFinishedAction?.Invoke();
      return;
    }

    _video = _videos[++_idxVideo];
    _rotateTransform.Angle = _video.Rotation;
    Source = new(_video.FilePath);
    Play();
    Stop();
  }

  private void OnMediaOpened() {
    if (NaturalVideoWidth + NaturalVideoHeight == 0) {
      NextVideo();
      return;
    }

    _hash = 0;
    _idxFrame = -1;
    Width = NaturalVideoWidth;
    Height = NaturalVideoHeight;
    Dispatcher.BeginInvoke(DispatcherPriority.Loaded, NextFrame);
  }

  private void NextFrame() {
    if (_idxFrame + 1 > _video.Frames.Count - 1) {
      _timeOut.Stop();
      NextVideo();
      return;
    }

    var oldPos = _frame?.Position ?? 0;
    _frame = _video.Frames[++_idxFrame];
    if (_idxFrame > 0 && _frame.Position == oldPos) {
      SaveFrame();
      NextFrame();
      return;
    }

    Position = new(0, 0, 0, 0, _frame.Position);
    _timeOut.Reset();
    _timeOut.Start();
    _positionTimer.Start();
  }

  private void OnPositionTimerTick() {
    var hash = this.ToBitmap().GetAvgHash();
    if (MH.Utils.Imaging.CompareHashes(_hash, hash) == 0 && _timeOut.ElapsedMilliseconds < 1000)
      return;

    _positionTimer.Stop();
    _hash = hash;
    SaveFrame();
    NextFrame();
  }

  private void SaveFrame() {
    Crop(this.ToBitmap()).Resize(_frame.Size).SaveAsJpeg(_frame.FilePath, _frame.Quality);
    _onSaveAction?.Invoke(_frame);
  }

  private BitmapSource Crop(BitmapSource bmp) {
    var rect = new Int32Rect(_frame.X, _frame.Y, _frame.Width, _frame.Height);
    if (!rect.HasArea) return bmp;
    if (ActualWidth >= Width && ActualHeight >= Height) return bmp.Crop(rect);
    return bmp.Crop(rect.Scale(ActualWidth / Width));
  }
}