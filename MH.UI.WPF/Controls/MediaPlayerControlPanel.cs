using MH.Utils.BaseClasses;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MH.UI.WPF.Controls {
  public enum TimelineShift { Beginning, LargeBack, SmallBack, SmallForward, LargeForward, End }

  public class MediaPlayerControlPanel : Control {
    public static readonly DependencyProperty PlayerProperty =
      DependencyProperty.Register(nameof(Player), typeof(MediaElement), typeof(MediaPlayerControlPanel),
        new((o, e) => (o as MediaPlayerControlPanel)?.SetPlayer()));
    public static readonly DependencyProperty TimelinePositionProperty =
      DependencyProperty.Register(nameof(TimelinePosition), typeof(double), typeof(MediaPlayerControlPanel));
    public static readonly DependencyProperty TimelineMaximumProperty =
      DependencyProperty.Register(nameof(TimelineMaximum), typeof(double), typeof(MediaPlayerControlPanel));
    public static readonly DependencyProperty TimelineSmallChangeProperty =
      DependencyProperty.Register(nameof(TimelineSmallChange), typeof(double), typeof(MediaPlayerControlPanel));
    public static readonly DependencyProperty TimelineLargeChangeProperty =
      DependencyProperty.Register(nameof(TimelineLargeChange), typeof(double), typeof(MediaPlayerControlPanel));
    public static readonly DependencyProperty PositionSlashDurationProperty =
      DependencyProperty.Register(nameof(PositionSlashDuration), typeof(string), typeof(MediaPlayerControlPanel));
    public static readonly DependencyProperty VolumeProperty =
      DependencyProperty.Register(nameof(Volume), typeof(double), typeof(MediaPlayerControlPanel), new(0.5));
    public static readonly DependencyProperty SpeedProperty =
      DependencyProperty.Register(nameof(Speed), typeof(double), typeof(MediaPlayerControlPanel), new(1.0));
    public static readonly DependencyProperty IsPlayingProperty =
      DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(MediaPlayerControlPanel),
        new((o, e) => (o as MediaPlayerControlPanel)?.IsPlayingChanged()));
    public static readonly DependencyProperty SourcePathProperty =
      DependencyProperty.Register(nameof(SourcePath), typeof(string), typeof(MediaPlayerControlPanel),
        new((o, e) => (o as MediaPlayerControlPanel)?.SetSource()));
    
    public MediaElement Player {
      get => (MediaElement)GetValue(PlayerProperty);
      set => SetValue(PlayerProperty, value);
    }

    public double TimelinePosition {
      get => (double)GetValue(TimelinePositionProperty);
      set => SetValue(TimelinePositionProperty, value);
    }

    public double TimelineMaximum {
      get => (double)GetValue(TimelineMaximumProperty);
      set => SetValue(TimelineMaximumProperty, value);
    }

    public double TimelineSmallChange {
      get => (double)GetValue(TimelineSmallChangeProperty);
      set => SetValue(TimelineSmallChangeProperty, value);
    }

    public double TimelineLargeChange {
      get => (double)GetValue(TimelineLargeChangeProperty);
      set => SetValue(TimelineLargeChangeProperty, value);
    }

    public string PositionSlashDuration {
      get => (string)GetValue(PositionSlashDurationProperty);
      set => SetValue(PositionSlashDurationProperty, value);
    }

    public double Volume {
      get => (double)GetValue(VolumeProperty);
      set => SetValue(VolumeProperty, value);
    }

    public double Speed {
      get => (double)GetValue(SpeedProperty);
      set => SetValue(SpeedProperty, value);
    }

    public bool IsPlaying {
      get => (bool)GetValue(IsPlayingProperty);
      set => SetValue(IsPlayingProperty, value);
    }

    public string SourcePath {
      get => (string)GetValue(SourcePathProperty);
      set => SetValue(SourcePathProperty, value);
    }

    public RelayCommand<TimelineShift> TimelineShiftCommand { get; }
    public RelayCommand<object> PlayPauseToggleCommand { get; }

    private const string _zeroTime = "00:00:00";
    private string _duration;
    private bool _isTimelineTimerExecuting;
    private bool _wasPlaying;
    private readonly DispatcherTimer _timelineTimer;

    static MediaPlayerControlPanel() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(MediaPlayerControlPanel),
        new FrameworkPropertyMetadata(typeof(MediaPlayerControlPanel)));
    }

    public MediaPlayerControlPanel() {
      TimelineShiftCommand = new(ShiftTimeline);
      PlayPauseToggleCommand = new(PlayPauseToggle);

      _timelineTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
      _timelineTimer.Tick += delegate {
        _isTimelineTimerExecuting = true;
        TimelinePosition = Math.Round(Player.Position.TotalMilliseconds / TimelineSmallChange) * TimelineSmallChange;
        _isTimelineTimerExecuting = false;
      };
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      if (Template.FindName("PART_TimelineSlider", this) is Slider slider) {
        slider.ValueChanged += (_, e) => {
          UpdatePositionSlashDuration();

          if (_isTimelineTimerExecuting) return;

          Player.Position = new(0, 0, 0, 0, (int)e.NewValue);
        };

        slider.PreviewMouseLeftButtonUp += delegate {
          if (_wasPlaying)
            IsPlaying = true;
        };

        slider.PreviewMouseLeftButtonDown += delegate {
          _wasPlaying = IsPlaying;
          if (IsPlaying)
            IsPlaying = false;
        };
      }
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

    public void UpdatePositionSlashDuration() {
      var position = Player == null
        ? _zeroTime
        : (new TimeSpan(0, 0, 0, (int)Math.Round(Player.Position.TotalSeconds), 0)).ToString();

      PositionSlashDuration = $"{position} / {_duration}";
    }

    public void SetSource() {
      if (Player == null) return;

      if (String.IsNullOrEmpty(SourcePath)) {
        Player.Source = null;
        IsPlaying = false;
      }
      else {
        Player.Source = new(SourcePath);
        IsPlaying = true;
      }
    }

    public void IsPlayingChanged() {
      if (IsPlaying) {
        Player.Play();
        _timelineTimer.Start();
      }
      else {
        Player.Pause();
        _timelineTimer.Stop();
      }
    }

    public void SetPlayer() {
      if (Player == null) return;

      Player.MediaOpened += delegate {
        if (!Player.HasVideo) return;

        var nd = Player.NaturalDuration;
        _duration = nd.HasTimeSpan ? nd.TimeSpan.ToString() : _zeroTime;
        TimelineMaximum = nd.HasTimeSpan ? nd.TimeSpan.TotalMilliseconds : 1000;
        IsPlaying = true;
      };

      Player.MediaEnded += delegate {
        // if video doesn't have TimeSpan than is probably less than 1s long
        // and can't be repeated with MediaElement.Stop()/MediaElement.Play()
        Player.Position = TimeSpan.FromMilliseconds(1);
      };
    }
  }
}
