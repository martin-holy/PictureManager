using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MH.UI.WPF.BaseClasses;

namespace MH.UI.WPF.Controls {
  public enum TimelineShift { Beginning, LargeBack, SmallBack, SmallForward, LargeForward, End }
  public enum PlayType { Video, Clip, Clips, Group }

  public class VideoPlayer : Control, INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged(this, new(name));

    private readonly DispatcherTimer _mouseHideTimer;
    private readonly DispatcherTimer _timelineTimer;
    private readonly DispatcherTimer _clipTimer;
    private DateTime _mouseLastMove;
    private PlayType _playType = PlayType.Video;
    private bool _isMouseHidden;
    private bool _isTimelineTimerExecuting;
    private bool _isPlaying;
    private bool _wasPlaying;
    private int _repeatCount;
    private int _repeatForSeconds = 3; // 0 => infinity
    private double _rotation;
    private double _volume = 0.5;
    private double _speed = 1;
    private double _timelinePosition;
    private double _timelineMaximum;
    private double _timelineSmallChange;
    private double _timelineLargeChange = 1000;

    public RelayCommand<TimelineShift> TimelineShiftCommand { get; }
    public RelayCommand<object> PlayPauseToggleCommand { get; }
    public MediaElement Player { get; private set; }
    public Action RepeatEnded { get; set; }
    public Action<bool, bool> SelectNextClip { get; set; }
    public int ClipTimeStart { get; set; }
    public int ClipTimeEnd { get; set; }
    public int RepeatForSeconds { get => _repeatForSeconds; set { _repeatForSeconds = value; OnPropertyChanged(); } }
    public double Rotation { get => _rotation; set { _rotation = value; OnPropertyChanged(); } }
    public double Volume { get => _volume; set { _volume = value; OnPropertyChanged(); } }
    public double TimelinePosition { get => _timelinePosition; set { _timelinePosition = value; OnPropertyChanged(); } }
    public double TimelineMaximum { get => _timelineMaximum; set { _timelineMaximum = value; OnPropertyChanged(); } }
    public double TimelineSmallChange { get => _timelineSmallChange; set { _timelineSmallChange = value; OnPropertyChanged(); } }
    public double TimelineLargeChange { get => _timelineLargeChange; set { _timelineLargeChange = value; OnPropertyChanged(); } }

    public double Speed {
      get => _speed;
      set {
        _speed = value;
        Player.SpeedRatio = value; // binding in XAML doesn't work
        StartClipTimer();
        OnPropertyChanged();
      }
    }

    public bool IsPlaying {
      get => _isPlaying;
      set {
        _isPlaying = value;

        if (value) {
          StartClipTimer();
          Player.Play();
          _timelineTimer.Start();
          _mouseHideTimer.Start();
        }
        else {
          Player.Pause();
          _timelineTimer.Stop();
          _mouseHideTimer.Stop();
          _clipTimer.Stop();
        }

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

    public string PositionSlashDuration =>
      $"{new TimeSpan(0, 0, 0, (int)Math.Round(Player.Position.TotalSeconds), 0)} / " +
      $"{(Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan.ToString() : "00:00:00")}";

    static VideoPlayer() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(VideoPlayer),
        new FrameworkPropertyMetadata(typeof(VideoPlayer)));
    }

    public VideoPlayer() {
      TimelineShiftCommand = new(ShiftTimeline);
      PlayPauseToggleCommand = new(PlayPauseToggle);

      MouseMove += (_, _) => {
        _mouseLastMove = DateTime.Now;
        if (_isMouseHidden) {
          _isMouseHidden = false;
          Cursor = Cursors.Arrow;
        }
      };

      MouseEnter += (o, _) => {
        var mec = (VideoPlayer)o;
        mec.KeyDown += VideoPlayerControl_OnKeyDown;
        mec.Focus();
      };

      MouseLeave += (o, _) => {
        var mec = (VideoPlayer)o;
        mec.KeyDown -= VideoPlayerControl_OnKeyDown;
      };

      _mouseHideTimer = new() { Interval = TimeSpan.FromMilliseconds(3000) };
      _mouseHideTimer.Tick += MouseHideTimerOnTick;

      _timelineTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
      _timelineTimer.Tick += TimelineTimerOnTick;

      _clipTimer = new() { Interval = TimeSpan.FromMilliseconds(10) };
      _clipTimer.Tick += ClipTimerOnTick;
    }

    private void MouseHideTimerOnTick(object sender, EventArgs e) {
      if (_isMouseHidden || DateTime.Now - _mouseLastMove < _mouseHideTimer.Interval) return;
      _isMouseHidden = true;
      Cursor = Cursors.None;
    }

    private void TimelineTimerOnTick(object sender, EventArgs e) {
      _isTimelineTimerExecuting = true;
      TimelinePosition = Math.Round(Player.Position.TotalMilliseconds / TimelineSmallChange) * TimelineSmallChange;
      _isTimelineTimerExecuting = false;
    }

    private void ClipTimerOnTick(object sender, EventArgs e) {
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

    public void SetNullSource() {
      Player.Source = null;
      IsPlaying = false;
    }

    public void SetSource(string filePath, double rotation, double smallChange) {
      TimelineSmallChange = smallChange;
      ClipTimeStart = 0;
      ClipTimeEnd = 0;
      Rotation = rotation;
      Player.Source = new(filePath);
      IsPlaying = true;
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
      _clipTimer.Interval = TimeSpan.FromMilliseconds(duration);
      _clipTimer.Start();
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      if (Template.FindName("PART_MediaElement", this) is MediaElement mediaElement) {
        Player = mediaElement;
        Player.MediaOpened += MediaElement_OnMediaOpened;
        Player.MediaEnded += MediaElement_OnMediaEnded;
        Player.MouseLeftButtonUp += MediaElement_OnMouseLeftButtonUp;
      }

      if (Template.FindName("PART_TimelineSlider", this) is Slider timelineSlider) {
        timelineSlider.ValueChanged += TimelineSlider_OnValueChanged;
        timelineSlider.PreviewMouseLeftButtonUp += TimelineSlider_OnPreviewMouseLeftButtonUp;
        timelineSlider.PreviewMouseLeftButtonDown += TimelineSlider_OnPreviewMouseLeftButtonDown;
      }
    }

    private void VideoPlayerControl_OnKeyDown(object sender, KeyEventArgs e) {
      switch (e.Key) {
        case Key.Home: ShiftTimeline(TimelineShift.Beginning); break;
        case Key.End: ShiftTimeline(TimelineShift.End); break;
        case Key.PageUp: ShiftTimeline(TimelineShift.LargeBack); break;
        case Key.PageDown: ShiftTimeline(TimelineShift.LargeForward); break;
        case Key.NumPad7: ShiftTimeline(TimelineShift.SmallBack); break;
        case Key.NumPad9: ShiftTimeline(TimelineShift.SmallForward); break;
        case Key.Space: PlayPauseToggle(); break;
      }
    }

    private void MediaElement_OnMediaOpened(object sender, RoutedEventArgs e) {
      if (!Player.HasVideo) return;

      var nd = Player.NaturalDuration;
      _repeatCount = (int)Math.Round(RepeatForSeconds / (nd.HasTimeSpan ? nd.TimeSpan.TotalMilliseconds / 1000.0 : 1), 0);
      TimelineMaximum = nd.HasTimeSpan ? nd.TimeSpan.TotalMilliseconds : 1000;
      IsPlaying = true;

      if (PlayType != PlayType.Video)
        SelectNextClip?.Invoke(false, true);
    }

    private void MediaElement_OnMediaEnded(object sender, RoutedEventArgs e) {
      // if video doesn't have TimeSpan than is probably less than 1s long
      // and can't be repeated with MediaElement.Stop()/MediaElement.Play()
      Player.Position = TimeSpan.FromMilliseconds(1);

      if (_repeatCount > 0)
        _repeatCount--;
      else
        RepeatEnded?.Invoke();
    }

    private void MediaElement_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
      PlayPauseToggle();

    private void TimelineSlider_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      _wasPlaying = IsPlaying;
      if (IsPlaying)
        IsPlaying = false;
    }

    private void TimelineSlider_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      if (_wasPlaying)
        IsPlaying = true;
    }

    private void TimelineSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
      OnPropertyChanged(nameof(PositionSlashDuration));

      if (_isTimelineTimerExecuting) return;

      Player.Position = new(0, 0, 0, 0, (int)e.NewValue);
    }

    private void ShiftTimeline(TimelineShift mode) {
      TimelinePosition = mode switch {
        TimelineShift.Beginning => 0,
        TimelineShift.LargeBack => TimelinePosition - TimelineLargeChange,
        TimelineShift.SmallBack => TimelinePosition - TimelineSmallChange,
        TimelineShift.SmallForward => TimelinePosition + TimelineSmallChange,
        TimelineShift.LargeForward => TimelinePosition + TimelineLargeChange,
        TimelineShift.End => TimelineMaximum,
        _ => throw new NotImplementedException()
      };
    }

    private void PlayPauseToggle() =>
      IsPlaying = !IsPlaying;
  }
}
