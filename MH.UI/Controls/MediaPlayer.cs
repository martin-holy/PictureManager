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

  private IPlatformSpecificUiMediaPlayer _uiPlayer;
  private readonly Timer _clipTimer;
  private readonly Timer _timelineTimer;
  private IVideoItem _currentItem;
  private PlayType _playType = PlayType.Video;
  private bool _autoPlay = true;
  private bool _isMuted;
  private bool _isPlaying;
  private bool _isTimelineTimerExecuting;
  private bool _wasPlaying;
  private int _clipTimeStart;
  private int _clipTimeEnd;
  private int _repeatCount;
  private int _repeatForSeconds = 3; // 0 => infinity
  private double _speed = 1;
  private double _timelinePosition;
  private double _timelineMaximum;
  private double _timelineSmallChange = 33;
  private double _timelineLargeChange = 1000;
  private double _volume = 0.5;
  private string _source;

  public static KeyValuePair<PlayType, string>[] PlayTypes { get; } = {
    new(PlayType.Video, "Video"),
    new(PlayType.Clip, "Clip"),
    new(PlayType.Clips, "Clips"),
    new(PlayType.Group, "Group")
  };

  public IVideoItem CurrentItem { get => _currentItem; private set { _currentItem = value; OnPropertyChanged(); } }
  public bool AutoPlay { get => _autoPlay; set { _autoPlay = value; OnPropertyChanged(); } }
  public int RepeatForSeconds { get => _repeatForSeconds; set { _repeatForSeconds = value; OnPropertyChanged(); } }
  public double TimelineSmallChange { get => _timelineSmallChange; set { _timelineSmallChange = value; OnPropertyChanged(); } }
  public double TimelineLargeChange { get => _timelineLargeChange; set { _timelineLargeChange = value; OnPropertyChanged(); } }

  public double Volume {
    get => _volume;
    set {
      _volume = value;
      if (_uiPlayer != null) _uiPlayer.Volume = value;
      OnPropertyChanged();
    }
  }

  public bool IsMuted {
    get => _isMuted;
    set {
      _isMuted = value;
      if (_uiPlayer != null) _uiPlayer.IsMuted = value;
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

      if (!_isTimelineTimerExecuting && _uiPlayer != null)
        _uiPlayer.Position = TimeSpan.FromMilliseconds((int)value);

      OnPropertyChanged();
      OnPropertyChanged(nameof(PositionSlashDuration));
    }
  }

  public double TimelineMaximum {
    get => _timelineMaximum;
    private set {
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
        if (_uiPlayer != null) _uiPlayer.Source = null;
        IsPlaying = false;
        TimelineMaximum = 0;
      }
      else if (_uiPlayer != null)
        _uiPlayer.Source = new(Source);

      OnPropertyChanged();
    }
  }

  public double Speed {
    get => _speed;
    set {
      _speed = Math.Round(value, 1);
      if (IsPlaying) StartClipTimer();
      if (_uiPlayer != null) _uiPlayer.SpeedRatio = _speed;
      OnPropertyChanged();
    }
  }

  public bool IsPlaying {
    get => _isPlaying;
    set {
      if (value) {
        _uiPlayer?.Play();
        StartClipTimer();
        _timelineTimer.Start();
      }
      else {
        _uiPlayer?.Pause();
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

  public RelayCommand DeleteItemCommand { get; }
  public RelayCommand PauseCommand { get; }
  public RelayCommand PlayCommand { get; }
  public RelayCommand SeekToEndCommand { get; }
  public RelayCommand SeekToStartCommand { get; }
  public RelayCommand SetEndMarkerCommand { get; }
  public RelayCommand SetNewClipCommand { get; }
  public RelayCommand SetNewImageCommand { get; }
  public RelayCommand SetStartMarkerCommand { get; }
  public RelayCommand TimelineShiftBeginningCommand { get; }
  public RelayCommand TimelineShiftEndCommand { get; }
  public RelayCommand TimelineShiftLargeBackCommand { get; }
  public RelayCommand TimelineShiftLargeForwardCommand { get; }
  public RelayCommand TimelineShiftSmallBackCommand { get; }
  public RelayCommand TimelineShiftSmallForwardCommand { get; }
  public RelayCommand TimelineSliderChangeEndedCommand { get; }
  public RelayCommand TimelineSliderChangeStartedCommand { get; }
  public RelayCommand<PropertyChangedEventArgs<double>> TimelineSliderValueChangedCommand { get; }

  public Func<int, IVideoClip> GetNewClipFunc { get; set; }
  public Func<int, IVideoImage> GetNewImageFunc { get; set; }
  public Action<bool, bool> SelectNextItemAction { get; set; }
  public Action OnItemDeleteAction { get; set; }

  public event EventHandler<ObjectEventArgs<Tuple<IVideoItem, bool>>> MarkerSetEvent = delegate { };
  public event EventHandler RepeatEndedEvent = delegate { };

  public MediaPlayer() {
    _clipTimer = new() { Interval = 10 };
    _clipTimer.Elapsed += delegate { OnClipTimer(); };
    _timelineTimer = new() { Interval = 250 };
    _timelineTimer.Elapsed += delegate { OnTimelineTimer(); };

    DeleteItemCommand = new(ItemDelete, () => CurrentItem != null, Res.IconXCross, "Delete");
    PauseCommand = new(() => IsPlaying = false, Res.IconPause, "Pause");
    PlayCommand = new(() => IsPlaying = true, Res.IconPlay, "Play");
    SeekToEndCommand = new(() => SeekTo(false), () => CurrentItem is IVideoClip, Res.IconChevronLeft, "Seek to end");
    SeekToStartCommand = new(() => SeekTo(true), () => CurrentItem != null, Res.IconChevronRight, "Seek to start");
    SetEndMarkerCommand = new(() => SetMarker(false), () => CurrentItem is IVideoClip, Res.IconChevronDown, "Set end");
    SetNewClipCommand = new(SetNewClip, () => !string.IsNullOrEmpty(Source), Res.IconMovieClapper, "Create new or close video clip");
    SetNewImageCommand = new(SetNewImage, () => !string.IsNullOrEmpty(Source), Res.IconImage, "Create new video image");
    SetStartMarkerCommand = new(() => SetMarker(true), () => CurrentItem != null, Res.IconChevronDown, "Set start");
    TimelineShiftBeginningCommand = new(() => ShiftTimeline(TimelineShift.Beginning), Res.IconTimelineShiftBeginning);
    TimelineShiftEndCommand = new(() => ShiftTimeline(TimelineShift.End), Res.IconTimelineShiftEnd);
    TimelineShiftLargeBackCommand = new(() => ShiftTimeline(TimelineShift.LargeBack), Res.IconTimelineShiftLargeBack);
    TimelineShiftLargeForwardCommand = new(() => ShiftTimeline(TimelineShift.LargeForward), Res.IconTimelineShiftLargeForward);
    TimelineShiftSmallBackCommand = new(() => ShiftTimeline(TimelineShift.SmallBack), Res.IconTimelineShiftSmallBack);
    TimelineShiftSmallForwardCommand = new(() => ShiftTimeline(TimelineShift.SmallForward), Res.IconTimelineShiftSmallForward);
    TimelineSliderChangeEndedCommand = new(TimelineSliderChangeEnded);
    TimelineSliderChangeStartedCommand = new(TimelineSliderChangeStarted);
    TimelineSliderValueChangedCommand = new(TimelineSliderValueChanged);
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
    IsPlaying = AutoPlay;

    if (!AutoPlay)
      ShiftTimeline(TimelineShift.Beginning);

    if (PlayType != PlayType.Video)
      SelectNextItemAction?.Invoke(false, true);
  }

  public void OnMediaEnded() {
    // if video doesn't have TimeSpan than is probably less than 1s long
    // and can't be repeated with WPF MediaElement.Stop()/MediaElement.Play()
    if (_uiPlayer != null) _uiPlayer.Position = TimeSpan.FromMilliseconds(1);

    if (_repeatCount > 0)
      _repeatCount--;
    else
      RepeatEndedEvent(this, EventArgs.Empty);
  }

  private void OnClipTimer() {
    Tasks.RunOnUiThread(() => {
      if (PlayType == PlayType.Video || _clipTimeEnd <= _clipTimeStart) return;

      switch (PlayType) {
        case PlayType.Clip:
          TimelinePosition = _clipTimeStart;
          break;

        case PlayType.Clips:
        case PlayType.Group:
          if (_repeatCount > 0) {
            _repeatCount--;
            TimelinePosition = _clipTimeStart;
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
      TimelinePosition = Math.Round(_uiPlayer?.Position.TotalMilliseconds ?? 0);

      // in case when UiMediaPlayer reports wrong video duration OnMediaOpened
      // TODO make it more precise. The true duration is still unknown
      if (TimelinePosition > TimelineMaximum)
        TimelineMaximum = TimelinePosition;

      _isTimelineTimerExecuting = false;
    });
  }

  private void StartClipTimer() {
    _clipTimer.Stop();

    if (PlayType == PlayType.Video || _clipTimeEnd <= _clipTimeStart) return;

    var duration = (_clipTimeEnd - _clipTimeStart) / Speed;
    if (duration <= 0) return;

    _repeatCount = PlayType is PlayType.Clips or PlayType.Group
      ? (int)Math.Round(RepeatForSeconds / (duration / 1000.0), 0)
      : 0;
    TimelinePosition = _clipTimeStart;
    _clipTimer.Interval = duration;
    _clipTimer.Start();
  }

  public void SetCurrent(IVideoItem item) {
    CurrentItem = item;
    if (item == null) {
      _clipTimeStart = 0;
      _clipTimeEnd = 0;
      return;
    }

    switch (item) {
      case IVideoClip vc: SetCurrentVideoClip(vc); break;
      case IVideoImage: SetCurrentVideoImage(); break;
    }

    SeekTo(true);
  }

  private void SetCurrentVideoImage() {
    if (!IsPlaying || PlayType == PlayType.Video) return;
    IsPlaying = false;
    if (PlayType == PlayType.Clip) return;
    Task.Run(() => {
      Thread.Sleep(RepeatForSeconds * 1000);
      Tasks.RunOnUiThread(() => {
        IsPlaying = true;
        SelectNextItemAction?.Invoke(PlayType == PlayType.Group, false);
      });
    });
  }

  private void SetCurrentVideoClip(IVideoClip vc) {
    _clipTimeStart = vc.TimeStart;
    _clipTimeEnd = vc.TimeEnd;

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
    var ms = GetPosition();
    switch (CurrentItem) {
      case IVideoClip vc: SetClipMarker(vc, ms, start); break;
      case IVideoImage vi: SetImageMarker(vi, ms); break;
    }
  }

  private void SetClipMarker(IVideoClip vc, int ms, bool start) {
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

    _clipTimeStart = vc.TimeStart;
    _clipTimeEnd = vc.TimeEnd;

    MarkerSetEvent(this, new(new(vc, start)));
  }

  private void SetImageMarker(IVideoItem vi, int ms) {
    vi.TimeStart = ms;
    MarkerSetEvent(this, new(new(vi, false)));
  }

  private void SetNewClip() {
    var ms = GetPosition();
    var vc = CurrentItem as IVideoClip;
    if (vc?.TimeEnd == 0)
      SetClipMarker(vc, ms, false);
    else {
      vc = GetNewClipFunc(ms);
      CurrentItem = vc;
      SetClipMarker(vc, ms, true);
    }
  }

  private void SetNewImage() {
    var ms = GetPosition();
    var vi = GetNewImageFunc(ms);
    CurrentItem = vi;
    SetImageMarker(vi, ms);
  }

  private int GetPosition() =>
    (int)Math.Round(TimelinePosition);

  private void ShiftTimeline(TimelineShift mode) =>
    TimelinePosition = mode switch {
      TimelineShift.Beginning => 0,
      TimelineShift.LargeBack => Math.Max(TimelinePosition - TimelineLargeChange, 0),
      TimelineShift.SmallBack => Math.Max(TimelinePosition - TimelineSmallChange, 0),
      TimelineShift.SmallForward => Math.Min(TimelinePosition + TimelineSmallChange, TimelineMaximum),
      TimelineShift.LargeForward => Math.Min(TimelinePosition + TimelineLargeChange, TimelineMaximum),
      TimelineShift.End => TimelineMaximum,
      _ => throw new NotImplementedException()
    };

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

  public void SetView(IPlatformSpecificUiMediaPlayer view) {
    if (_uiPlayer != null) {
      _uiPlayer.Pause();
      _uiPlayer.Source = null;
      _uiPlayer.ViewModel = null;
    }

    _uiPlayer = view;
    if (view == null) return;
    view.ViewModel = this;
    view.SpeedRatio = Speed;
    view.Volume = Volume;
    view.IsMuted = IsMuted;
    if (!string.IsNullOrEmpty(Source))
      view.Source = new(Source);
  }
}