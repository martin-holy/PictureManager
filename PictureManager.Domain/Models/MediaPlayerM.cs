using MH.Utils.BaseClasses;
using System;
using System.Timers;

namespace PictureManager.Domain.Models {
  public enum TimelineShift { Beginning, LargeBack, SmallBack, SmallForward, LargeForward, End }
  public enum PlayType { Video, Clip, Clips, Group }

  public sealed class MediaPlayerM : ObservableObject {
    private readonly Timer _clipTimer;
    private PlayType _playType = PlayType.Video;
    private bool _isPlaying;
    private bool _isMuted;
    private int _repeatCount;
    private int _repeatForSeconds = 3; // 0 => infinity
    private double _volume = 0.5;
    private double _speed = 1;
    private double _timelinePosition;
    private double _timelineMaximum;
    private double _timelineSmallChange = 33;
    private double _timelineLargeChange = 1000;
    private string _source;

    public Action RepeatEnded { get; set; }
    public Action<bool, bool> SelectNextClip { get; set; }
    public int ClipTimeStart { get; set; }
    public int ClipTimeEnd { get; set; }
    public int RepeatForSeconds { get => _repeatForSeconds; set { _repeatForSeconds = value; OnPropertyChanged(); } }
    public double Volume { get => _volume; set { _volume = value; OnPropertyChanged(); } }
    public double TimelinePosition { get => _timelinePosition; set { _timelinePosition = value; OnPropertyChanged(); } }
    public double TimelineMaximum {
      get => _timelineMaximum;
      set {
        _timelineMaximum = value;
        _repeatCount = (int)Math.Round(RepeatForSeconds / (value / 1000), 0);
        OnPropertyChanged();
      }
    }
    public double TimelineSmallChange { get => _timelineSmallChange; set { _timelineSmallChange = value; OnPropertyChanged(); } }
    public double TimelineLargeChange { get => _timelineLargeChange; set { _timelineLargeChange = value; OnPropertyChanged(); } }
    public string Source { get => _source; set { _source = value; OnPropertyChanged(); } }
    public bool IsMuted { get => _isMuted; set { _isMuted = value; OnPropertyChanged(); } }

    public double Speed {
      get => _speed;
      set {
        _speed = value;
        StartClipTimer();
        OnPropertyChanged();
      }
    }

    public bool IsPlaying {
      get => _isPlaying;
      set {
        if (value)
          StartClipTimer();
        else
          _clipTimer.Stop();

        _isPlaying = value;
        OnPropertyChanged();
      }
    }

    public PlayType PlayType {
      get => _playType;
      set {
        _playType = value;
        StartClipTimer();
        OnPropertyChanged();
      }
    }

    public RelayCommand<object> MediaOpenedCommand { get; }
    public RelayCommand<object> MediaEndedCommand { get; }

    public MediaPlayerM() {
      _clipTimer = new() { Interval = 10 };
      _clipTimer.Elapsed += ClipTimerOnTick;

      MediaOpenedCommand = new(() => {
        if (PlayType != PlayType.Video)
          SelectNextClip?.Invoke(false, true);
      });

      MediaEndedCommand = new(() => {
        if (_repeatCount > 0)
          _repeatCount--;
        else
          RepeatEnded?.Invoke();
      });
    }

    ~MediaPlayerM() {
      _clipTimer?.Dispose();
    }

    private void ClipTimerOnTick(object sender, ElapsedEventArgs e) {
      if (PlayType == PlayType.Video || ClipTimeEnd <= ClipTimeStart) return;

      switch (PlayType) {
        case PlayType.Clip:
          TimelinePosition = ClipTimeStart;
          break;

        case PlayType.Clips:
        case PlayType.Group:
          if (_repeatCount > 0) {
            _repeatCount--;
            TimelinePosition = ClipTimeStart;
          }
          else {
            SelectNextClip?.Invoke(PlayType == PlayType.Group, false);
          }

          break;
      }
    }

    public void StartClipTimer() {
      _clipTimer.Stop();

      if (PlayType == PlayType.Video || ClipTimeEnd <= ClipTimeStart) return;

      var duration = (ClipTimeEnd - ClipTimeStart) / Speed;

      if (duration <= 0) return;

      _repeatCount = PlayType is PlayType.Clips or PlayType.Group
        ? (int)Math.Round(RepeatForSeconds / (duration / 1000.0), 0)
        : 0;
      TimelinePosition = ClipTimeStart;
      _clipTimer.Interval = duration;
      _clipTimer.Start();
    }
  }
}
