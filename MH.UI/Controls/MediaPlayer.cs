using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace MH.UI.Controls;

public enum TimelineShift { Beginning, LargeBack, SmallBack, SmallForward, LargeForward, End }
public enum PlayType { Video, Clip, Clips, Group }

public sealed class MediaPlayer : ObservableObject {
  private const string _zeroTime = "00:00:00";

  private bool _isTimelineTimerExecuting;
  private bool _wasPlaying;
  private readonly Timer _clipTimer;
  private readonly Timer _timelineTimer;
  private IVideoItem _currentItem;
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

  public static KeyValuePair<PlayType, string>[] PlayTypes { get; } = {
    new(PlayType.Video, "Video"),
    new(PlayType.Clip, "Clip"),
    new(PlayType.Clips, "Clips"),
    new(PlayType.Group, "Group")
  };
  
  public IPlatformSpecificUiMediaPlayer UiMediaPlayer { get; set; }
  public IVideoItem CurrentItem { get => _currentItem; private set { _currentItem = value; OnPropertyChanged(); } }
  public bool IsPlayOnOpened { get; set; }
  public int ClipTimeStart { get; set; }
  public int ClipTimeEnd { get; set; }
  public int RepeatForSeconds { get => _repeatForSeconds; set { _repeatForSeconds = value; OnPropertyChanged(); } }
  public double TimelineSmallChange { get => _timelineSmallChange; set { _timelineSmallChange = value; OnPropertyChanged(); } }
  public double TimelineLargeChange { get => _timelineLargeChange; set { _timelineLargeChange = value; OnPropertyChanged(); } }

  public double Volume {
    get => _volume;
    set {
      _volume = value;
      if (UiMediaPlayer != null) UiMediaPlayer.Volume = value;
      OnPropertyChanged();
    }
  }

  public bool IsMuted {
    get => _isMuted;
    set {
      _isMuted = value;
      if (UiMediaPlayer != null) UiMediaPlayer.IsMuted = value;
      OnPropertyChanged();
    }
  }

  public string PositionSlashDuration =>
    $"{(string.IsNullOrEmpty(Source)
      ? _zeroTime
      : FormatPosition((int)TimelinePosition))} / {FormatPosition((int)TimelineMaximum)}";

  public double TimelinePosition {
    get => _timelinePosition;
    set {
      _timelinePosition = value;

      if (!_isTimelineTimerExecuting && UiMediaPlayer != null)
        UiMediaPlayer.Position = TimeSpan.FromMilliseconds((int)value);

      OnPropertyChanged();
      OnPropertyChanged(nameof(PositionSlashDuration));
    }
  }

  public double TimelineMaximum {
    get => _timelineMaximum;
    set {
      _timelineMaximum = value;
      _repeatCount = (int)Math.Round(RepeatForSeconds / (value / 1000), 0);
      OnPropertyChanged();
      OnPropertyChanged(nameof(PositionSlashDuration));
    }
  }

  public string Source {
    get => _source;
    set {
      _source = value;
      SetCurrent(null);

      if (string.IsNullOrEmpty(value)) {
        if (UiMediaPlayer != null) UiMediaPlayer.Source = null;
        IsPlaying = false;
        TimelineMaximum = 0;
      }
      else {
        if (UiMediaPlayer != null) UiMediaPlayer.Source = new(Source);
        IsPlaying = true;
      }

      OnPropertyChanged();
    }
  }

  public double Speed {
    get => _speed;
    set {
      _speed = Math.Round(value, 1);
      if (IsPlaying) StartClipTimer();
      if (UiMediaPlayer != null) UiMediaPlayer.SpeedRatio = _speed;
      OnPropertyChanged();
    }
  }

  public bool IsPlaying {
    get => _isPlaying;
    set {
      if (value) {
        UiMediaPlayer?.Play();
        StartClipTimer();
        _timelineTimer.Start();
      }
      else {
        UiMediaPlayer?.Pause();
        _clipTimer.Stop();
        _timelineTimer.Stop();
      }

      _isPlaying = value;
      OnPropertyChanged();
    }
  }

  public PlayType PlayType {
    get => _playType;
    set {
      _playType = value;
      if (IsPlaying) StartClipTimer();
      OnPropertyChanged();
    }
  }

  public RelayCommand<PlayType> SetPlayTypeCommand { get; }
  public RelayCommand<int> SeekToPositionCommand { get; }
  public RelayCommand SeekToStartCommand { get; }
  public RelayCommand SeekToEndCommand { get; }
  public RelayCommand SetStartMarkerCommand { get; }
  public RelayCommand SetEndMarkerCommand { get; }
  public RelayCommand SetNewClipCommand { get; }
  public RelayCommand SetNewImageCommand { get; }
  public RelayCommand DeleteItemCommand { get; }
  public RelayCommand PlayCommand { get; }
  public RelayCommand PauseCommand { get; }
  public RelayCommand PlayPauseToggleCommand { get; }
  public RelayCommand<PropertyChangedEventArgs<double>> TimelineSliderValueChangedCommand { get; }
  public RelayCommand TimelineSliderChangeStartedCommand { get; }
  public RelayCommand TimelineSliderChangeEndedCommand { get; }
  public RelayCommand TimelineShiftBeginningCommand { get; }
  public RelayCommand TimelineShiftLargeBackCommand { get; }
  public RelayCommand TimelineShiftSmallBackCommand { get; }
  public RelayCommand TimelineShiftSmallForwardCommand { get; }
  public RelayCommand TimelineShiftLargeForwardCommand { get; }
  public RelayCommand TimelineShiftEndCommand { get; }

  public Func<IVideoClip> GetNewClipFunc { get; set; }
  public Func<IVideoImage> GetNewImageFunc { get; set; }
  public Action<bool, bool> SelectNextItemAction { get; set; }
  public Action OnItemDeleteAction { get; set; }

  public event EventHandler<ObjectEventArgs<Tuple<IVideoItem, bool>>> MarkerSetEvent = delegate { };
  public event EventHandler RepeatEndedEvent = delegate { };

  public MediaPlayer() {
    _clipTimer = new() { Interval = 10 };
    _clipTimer.Elapsed += delegate { OnClipTimer(); };
    _timelineTimer = new() { Interval = 250 };
    _timelineTimer.Elapsed += delegate { OnTimelineTimer(); };

    SetPlayTypeCommand = new(x => PlayType = x);
    SeekToPositionCommand = new(x => TimelinePosition = x);
    SeekToStartCommand = new(() => SeekTo(true), () => CurrentItem != null, Res.IconChevronRight, "Seek to start");
    SeekToEndCommand = new(() => SeekTo(false), () => CurrentItem is IVideoClip, Res.IconChevronLeft, "Seek to end");
    SetStartMarkerCommand = new(() => SetMarker(true), () => CurrentItem != null, Res.IconChevronDown, "Set start");
    SetEndMarkerCommand = new(() => SetMarker(false), () => CurrentItem is IVideoClip, Res.IconChevronDown, "Set end");
    SetNewClipCommand = new(SetNewClip, () => !string.IsNullOrEmpty(Source), Res.IconMovieClapper, "Create new or close video clip");
    SetNewImageCommand = new(SetNewImage, () => !string.IsNullOrEmpty(Source), Res.IconImage, "Create new video image");
    DeleteItemCommand = new(ItemDelete, () => CurrentItem != null, Res.IconXCross, "Delete");
    PlayCommand = new(() => IsPlaying = true, Res.IconPlay, "Play");
    PauseCommand = new(() => IsPlaying = false, Res.IconPause, "Pause");
    PlayPauseToggleCommand = new(PlayPauseToggle);
    TimelineSliderValueChangedCommand = new(TimelineSliderValueChanged);
    TimelineSliderChangeStartedCommand = new(TimelineSliderChangeStarted);
    TimelineSliderChangeEndedCommand = new(TimelineSliderChangeEnded);
    TimelineShiftBeginningCommand = new(() => ShiftTimeline(TimelineShift.Beginning), Res.IconTimelineShiftBeginning);
    TimelineShiftLargeBackCommand = new(() => ShiftTimeline(TimelineShift.LargeBack), Res.IconTimelineShiftLargeBack);
    TimelineShiftSmallBackCommand = new(() => ShiftTimeline(TimelineShift.SmallBack), Res.IconTimelineShiftSmallBack);
    TimelineShiftSmallForwardCommand = new(() => ShiftTimeline(TimelineShift.SmallForward), Res.IconTimelineShiftSmallForward);
    TimelineShiftLargeForwardCommand = new(() => ShiftTimeline(TimelineShift.LargeForward), Res.IconTimelineShiftLargeForward);
    TimelineShiftEndCommand = new(() => ShiftTimeline(TimelineShift.End), Res.IconTimelineShiftEnd);
  }

  ~MediaPlayer() {
    _clipTimer?.Dispose();
    _timelineTimer?.Dispose();
  }

  private void TimelineSliderValueChanged(PropertyChangedEventArgs<double> value) {
    if (!_isTimelineTimerExecuting)
      TimelinePosition = value.NewValue;
  }

  private void TimelineSliderChangeStarted() {
    _wasPlaying = IsPlaying;
    if (IsPlaying)
      IsPlaying = false;
  }

  private void TimelineSliderChangeEnded() {
    if (_wasPlaying)
      IsPlaying = true;
  }

  private void ItemDelete() =>
    OnItemDeleteAction?.Invoke();

  public void OnMediaOpened(int duration) {
    TimelineMaximum = duration > 1000 ? duration : 1000;
    IsPlaying = IsPlayOnOpened;

    if (!IsPlayOnOpened)
      ShiftTimeline(TimelineShift.Beginning);

    if (PlayType != PlayType.Video)
      SelectNextItemAction?.Invoke(false, true);
  }

  public void OnMediaEnded() {
    // if video doesn't have TimeSpan than is probably less than 1s long
    // and can't be repeated with WPF MediaElement.Stop()/MediaElement.Play()
    if (UiMediaPlayer != null) UiMediaPlayer.Position = TimeSpan.FromMilliseconds(1);

    if (_repeatCount > 0)
      _repeatCount--;
    else
      RepeatEndedEvent(this, EventArgs.Empty);
  }

  private void OnClipTimer() {
    Tasks.RunOnUiThread(() => {
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
          else
            SelectNextItemAction?.Invoke(PlayType == PlayType.Group, false);

          break;
      }
    });
  }

  private void OnTimelineTimer() {
    Tasks.RunOnUiThread(() => {
      _isTimelineTimerExecuting = true;
      TimelinePosition = Math.Round(UiMediaPlayer?.Position.TotalMilliseconds ?? 0);

      // in case when UiMediaPlayer reports wrong video duration OnMediaOpened
      // TODO make it more precise. The true duration is still unknown
      if (TimelinePosition > TimelineMaximum)
        TimelineMaximum = TimelinePosition;

      _isTimelineTimerExecuting = false;
    });
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

  public void SetCurrent(IVideoItem item) {
    CurrentItem = item;
    if (item == null) {
      ClipTimeStart = 0;
      ClipTimeEnd = 0;
      return;
    }

    switch (item) {
      case IVideoClip vc: SetCurrentVideoClip(vc); break;
      case IVideoImage: SetCurrentVideoImage(); break;
    }

    SeekTo(true);
  }

  private void SetCurrentVideoImage() {
    if (!IsPlaying) return;
    IsPlaying = false;
    Task.Run(() => {
      Thread.Sleep(RepeatForSeconds * 1000);
      Tasks.RunOnUiThread(() => {
        IsPlaying = true;
        SelectNextItemAction?.Invoke(PlayType == PlayType.Group, false);
      });
    });
  }

  private void SetCurrentVideoClip(IVideoClip vc) {
    ClipTimeStart = vc.TimeStart;
    ClipTimeEnd = vc.TimeEnd;

    if (PlayType != PlayType.Video) {
      Volume = vc.Volume;
      Speed = vc.Speed;
    }

    if (IsPlaying)
      StartClipTimer();
  }

  private void SeekTo(bool start) =>
    TimelinePosition = start ? CurrentItem.TimeStart : ((IVideoClip)CurrentItem).TimeEnd;

  private void SetMarker(bool start) {
    switch (CurrentItem) {
      case IVideoClip vc: SetClipMarker(vc, start); break;
      case IVideoImage vi: SetImageMarker(vi); break;
    }
  }

  private void SetClipMarker(IVideoClip vc, bool start) {
    var ms = (int)Math.Round(TimelinePosition);

    if (start) {
      vc.TimeStart = ms;
      if (ms > vc.TimeEnd)
        vc.TimeEnd = 0;
    }
    else if (ms < vc.TimeStart) {
      vc.TimeEnd = vc.TimeStart;
      vc.TimeStart = ms;
    }
    else
      vc.TimeEnd = ms;

    vc.Volume = Volume;
    vc.Speed = Speed;

    ClipTimeStart = vc.TimeStart;
    ClipTimeEnd = vc.TimeEnd;

    MarkerSetEvent(this, new(new(vc, start)));
  }

  private void SetImageMarker(IVideoImage vi) {
    vi.TimeStart = (int)Math.Round(TimelinePosition);
    MarkerSetEvent(this, new(new(vi, false)));
  }

  private void SetNewClip() {
    var vc = CurrentItem as IVideoClip;
    if (vc?.TimeEnd == 0)
      SetClipMarker(vc, false);
    else {
      vc = GetNewClipFunc();
      CurrentItem = vc;
      SetClipMarker(vc, true);
    }
  }

  private void SetNewImage() {
    var vi = GetNewImageFunc();
    CurrentItem = vi;
    SetImageMarker(vi);
  }

  private void ShiftTimeline(TimelineShift mode) {
    TimelinePosition = mode switch {
      TimelineShift.Beginning => 0,
      TimelineShift.LargeBack => Math.Max(TimelinePosition - TimelineLargeChange, 0),
      TimelineShift.SmallBack => Math.Max(TimelinePosition - TimelineSmallChange, 0),
      TimelineShift.SmallForward => Math.Min(TimelinePosition + TimelineSmallChange, TimelineMaximum),
      TimelineShift.LargeForward => Math.Min(TimelinePosition + TimelineLargeChange, TimelineMaximum),
      TimelineShift.End => TimelineMaximum,
      _ => throw new NotImplementedException()
    };
  }

  private void PlayPauseToggle() =>
    IsPlaying = !IsPlaying;

  public static string FormatPosition(int ms) =>
    TimeSpan.FromMilliseconds(ms).ToString(
      ms >= 60 * 60 * 1000
        ? @"h\:mm\:ss\.fff"
        : @"m\:ss\.fff");

  public static string FormatDuration(int ms) =>
    ms < 0
      ? string.Empty
      : TimeSpan.FromMilliseconds(ms).ToString(
        ms >= 60 * 60 * 1000
          ? @"h\:mm\:ss\.f"
          : ms >= 60 * 1000
            ? @"m\:ss\.f"
            : @"s\.f\s");
}