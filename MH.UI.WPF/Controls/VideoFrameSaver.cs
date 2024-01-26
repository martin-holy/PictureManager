using MH.UI.HelperClasses;
using MH.UI.Interfaces;
using MH.UI.WPF.Extensions;
using MH.Utils;
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
  private bool _capture;
  private int _idxVideo;
  private int _idxFrame;
  private long _hash;
  private VfsVideo[] _videos;
  private VfsVideo _video;
  private VfsFrame _frame;
  private readonly Stopwatch _timeOut = new();
  private Action<VfsFrame> _onSaveAction;
  private Action<VfsFrame, Exception> _onErrorAction;
  private Action _onFinishedAction;

  static VideoFrameSaver() {
    DefaultStyleKeyProperty.OverrideMetadata(
      typeof(VideoFrameSaver),
      new FrameworkPropertyMetadata(typeof(VideoFrameSaver)));
  }

  public VideoFrameSaver() {
    LoadedBehavior = MediaState.Manual;
    UnloadedBehavior = MediaState.Close;
    IsMuted = true;
    Stretch = Stretch.Uniform;
    StretchDirection = StretchDirection.Both;
    ScrubbingEnabled = true;
    MediaOpened += delegate { OnMediaOpened(); };
  }

  public void Save(VfsVideo[] videos, Action<VfsFrame> onSaveAction, Action<VfsFrame, Exception> onErrorAction, Action onFinishedAction) {
    _videos = videos;
    _onSaveAction = onSaveAction;
    _onErrorAction = onErrorAction;
    _onFinishedAction = onFinishedAction;
    _idxVideo = -1;
    MaxWidth = ((FrameworkElement)Parent).ActualWidth;
    MaxHeight = ((FrameworkElement)Parent).ActualHeight;
    CompositionTarget.Rendering += CompositionTargetOnRendering;
    NextVideo();
  }

  private void NextVideo() {
    if (_idxVideo + 1 > _videos.Length - 1) {
      CompositionTarget.Rendering -= CompositionTargetOnRendering;
      Source = null;
      _frame = null;
      _video = null;
      _videos = null;
      _onFinishedAction?.Invoke();
      return;
    }

    _video = _videos[++_idxVideo];
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
    _frame = null;
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
    if (_frame.Position == oldPos) {
      SaveFrame();
      NextFrame();
      return;
    }

    if (_hash == 0) _hash = GetHash();
    Position = new(0, 0, 0, 0, _frame.Position);
    _timeOut.Reset();
    _timeOut.Start();
    _capture = true;
  }

  private void CompositionTargetOnRendering(object sender, EventArgs e) {
    if (!_capture) return;
    var hash = GetHash();
    if (_timeOut.ElapsedMilliseconds > 2000)
      Log.Error("VideoFrameSaver TimeOut", _frame.FilePath);
    else if (Imaging.CompareHashes(_hash, hash) == 0) return;
    _capture = false;
    _hash = hash;
    SaveFrame();
    NextFrame();
  }

  private void SaveFrame() {
    try {
      Crop(this.ToBitmap()).Resize(_frame.Size).SaveAsJpeg(_frame.FilePath, _frame.Quality);
      _onSaveAction?.Invoke(_frame);
    }
    catch (Exception ex) {
      _onErrorAction?.Invoke(_frame, ex);
    }
  }

  private BitmapSource Crop(BitmapSource bmp) {
    var rect = new Int32Rect(_frame.X, _frame.Y, _frame.Width, _frame.Height);
    if (!rect.HasArea) return bmp;
    if (ActualWidth >= Width && ActualHeight >= Height) return bmp.Crop(rect);
    return bmp.Crop(rect.Scale(ActualWidth / Width));
  }

  private long GetHash() {
    try {
      return this.ToBitmap().GetAvgHash();
    }
    catch (Exception ex) {
      Log.Error(ex);
      return 0;
    }
  }
}