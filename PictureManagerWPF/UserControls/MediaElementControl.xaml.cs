using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PictureManager.UserControls {
  public enum TimelineShift { LargeBack, SmallBack, SmallForward, LargeForward }

  public partial class MediaElementControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private readonly DispatcherTimer _timelineTimer;
    private bool _isTimelineTimerExecuting;
    private bool _wasPlaying;
    private int _repeatCount;

    public int RepeatForMilliseconds; // 0 => infinity
    public Action RepeatEnded;

    public string PositionSlashDuration {
      get {
        var pos = new TimeSpan(0, 0, 0, (int)Math.Round(MediaElement.Position.TotalSeconds), 0).ToString();
        var dur = MediaElement.NaturalDuration.HasTimeSpan ? MediaElement.NaturalDuration.TimeSpan.ToString() : "00:00:00";
        return $"{pos} / {dur}";
      }
    }

    private bool _isPlaying;

    public bool IsPlaying {
      get => _isPlaying;
      set {
        _isPlaying = value;
        OnPropertyChanged();

        if (value) {
          MediaElement.Play();
          _timelineTimer.Start();
        }
        else {
          _timelineTimer.Stop();
          MediaElement.Pause();
        }
      }
    }

    public MediaElementControl() {
      InitializeComponent();

      MediaElement.Volume = VolumeSlider.Value;
      MediaElement.SpeedRatio = SpeedSlider.Value;

      _timelineTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
      _timelineTimer.Tick += delegate {
        _isTimelineTimerExecuting = true;
        TimelineSlider.Value = MediaElement.Position.TotalMilliseconds;
        _isTimelineTimerExecuting = false;
      };

      MouseEnter += delegate (object sender, MouseEventArgs e) {
        var mec = (MediaElementControl)sender;
        mec.KeyDown += MediaElementControl_OnKeyDown;
        mec.Focus();
      };

      MouseLeave += delegate (object sender, MouseEventArgs e) {
        var mec = (MediaElementControl)sender;
        mec.KeyDown -= MediaElementControl_OnKeyDown;
      };
    }

    private void MediaElementControl_OnKeyDown(object sender, KeyEventArgs e) {
      switch (e.Key) {
        case Key.Left: {
          switch (Keyboard.Modifiers) {
            case ModifierKeys.Control: ShiftTimeline(TimelineShift.SmallBack); break;
            case ModifierKeys.Shift: ShiftTimeline(TimelineShift.LargeBack); break;
          }

          break;
        }
        case Key.Right: {
          switch (Keyboard.Modifiers) {
            case ModifierKeys.Control: ShiftTimeline(TimelineShift.SmallForward); break;
            case ModifierKeys.Shift: ShiftTimeline(TimelineShift.LargeForward); break;
          }

          break;
        }
        case Key.Space: {
          PlayPauseToggle();
          break;
        }
      }
    }

    private void MediaElement_OnMediaOpened(object sender, RoutedEventArgs e) {
      if (!MediaElement.HasVideo) return;

      var nd = MediaElement.NaturalDuration;
      _repeatCount = (int) Math.Round(RepeatForMilliseconds / (nd.HasTimeSpan ? nd.TimeSpan.TotalMilliseconds : 1000), 0);

      TimelineSlider.Maximum = nd.HasTimeSpan ? nd.TimeSpan.TotalMilliseconds: 0;
      IsPlaying = true;
    }

    private void MediaElement_OnMediaEnded(object sender, RoutedEventArgs e) {
      // if video doesn't have TimeSpan than is probably less than 1s long and can't be repeated
      if ((_repeatCount > 0 || RepeatForMilliseconds == 0) && MediaElement.NaturalDuration.HasTimeSpan) {
        _repeatCount--;
        MediaElement.Stop();
        MediaElement.Play();
        return;
      }

      RepeatEnded();
    }

    private void MediaElement_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      PlayPauseToggle();
    }

    private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> e) {
      MediaElement.Volume = VolumeSlider.Value;
    }

    private void ChangeMediaSpeed(object sender, RoutedPropertyChangedEventArgs<double> e) {
      MediaElement.SpeedRatio = SpeedSlider.Value;
    }

    private void TimelineSlider_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      _wasPlaying = IsPlaying;
      if (IsPlaying) IsPlaying = false;
    }

    private void TimelineSlider_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      if (_wasPlaying) IsPlaying = true;
    }

    private void TimelineSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
      OnPropertyChanged(nameof(PositionSlashDuration));

      if (_isTimelineTimerExecuting) return;

      MediaElement.Position = new TimeSpan(0, 0, 0, 0, (int)e.NewValue);
    }

    private void ShiftTimelineButton_OnClick(object sender, RoutedEventArgs e) {
      ShiftTimeline((TimelineShift)((Button)sender).Tag);
    }

    private void ShiftTimeline(TimelineShift mode) {
      switch (mode) {
        case TimelineShift.LargeBack:
          TimelineSlider.Value -= TimelineSlider.LargeChange;
          break;
        case TimelineShift.SmallBack:
          TimelineSlider.Value -= TimelineSlider.SmallChange;
          break;
        case TimelineShift.SmallForward:
          TimelineSlider.Value += TimelineSlider.SmallChange;
          break;
        case TimelineShift.LargeForward:
          TimelineSlider.Value += TimelineSlider.LargeChange;
          break;
      }
    }

    private void PlayPauseToggle() {
      IsPlaying = !IsPlaying;
    }

    private void PlayPauseToggle(object sender, RoutedEventArgs e) {
      PlayPauseToggle();
    }
  }
}
