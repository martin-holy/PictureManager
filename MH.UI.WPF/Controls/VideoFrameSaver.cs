using MH.UI.WPF.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MH.UI.WPF.Controls;

public class VideoFrameSaver : MediaElement {
  private int _idxVideo;
  private int _idxFrame;
  private long _hash;
  private KeyValuePair<string, KeyValuePair<int, string>[]> _currentVideo;
  private KeyValuePair<int, string> _currentFrame;
  private readonly DispatcherTimer _positionTimer;
  private readonly Stopwatch _timeOut = new();

  public static readonly DependencyProperty VideosProperty = DependencyProperty.Register(
    nameof(Videos), typeof(List<KeyValuePair<string, KeyValuePair<int, string>[]>>), typeof(VideoFrameSaver),
    new((o, _) => (o as VideoFrameSaver)?.Save()));

  public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
    nameof(Size), typeof(int), typeof(VideoFrameSaver));

  public static readonly DependencyProperty QualityProperty = DependencyProperty.Register(
    nameof(Quality), typeof(int), typeof(VideoFrameSaver));

  public static readonly DependencyProperty FinishedProperty = DependencyProperty.Register(
    nameof(Finished), typeof(Action), typeof(VideoFrameSaver));

  public List<KeyValuePair<string, KeyValuePair<int, string>[]>> Videos { get => (List<KeyValuePair<string, KeyValuePair<int, string>[]>>)GetValue(VideosProperty); set => SetValue(VideosProperty, value); }
  public int Size { get => (int)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
  public int Quality { get => (int)GetValue(QualityProperty); set => SetValue(QualityProperty, value); }
  public Action Finished { get => (Action)GetValue(FinishedProperty); set => SetValue(FinishedProperty, value); }

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
    MediaOpened += delegate { OnMediaOpened(); };
    _positionTimer = new() { Interval = TimeSpan.FromMilliseconds(1) };
    _positionTimer.Tick += delegate { OnPositionTimerTick(); };
  }

  public void Save() {
    _idxVideo = -1;
    NextVideo();
  }

  private void NextVideo() {
    if (_idxVideo + 1 > Videos.Count - 1) {
      Source = null;
      Finished?.Invoke();
      return;
    }

    _currentVideo = Videos[++_idxVideo];
    Source = new(_currentVideo.Key);
    Play();
    Stop();
  }

  private void OnMediaOpened() {
    _hash = 0;
    _idxFrame = -1;
    NextFrame();
  }

  private void NextFrame() {
    if (_idxFrame + 1 > _currentVideo.Value.Length - 1) {
      _timeOut.Stop();
      NextVideo();
      return;
    }

    _currentFrame = _currentVideo.Value[++_idxFrame];
    Position = new(0, 0, 0, 0, _currentFrame.Key);
    _timeOut.Reset();
    _timeOut.Start();
    _positionTimer.Start();
  }

  private void OnPositionTimerTick() {
    var hash = GetHash();

    if (MH.Utils.Imaging.CompareHashes(_hash, hash) == 0 && _timeOut.ElapsedMilliseconds < 1000)
      return;

    _positionTimer.Stop();
    Imaging.CreateThumbnailFromVisual(this, _currentFrame.Value, Size, Quality);
    _hash = hash;
    NextFrame();
  }

  private long GetHash() =>
    Imaging.GetBitmapAvgHash(Imaging.VisualToBitmapSource(this));
}