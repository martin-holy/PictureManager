using System.Windows;
using System.Windows.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using PictureManager.ViewModels;

namespace PictureManager.CustomControls {
  public enum TimelineShift { Beginning, LargeBack, SmallBack, SmallForward, LargeForward, End }
  public enum PlayType { Video, Clip, Clips, Group }

  public class VideoPlayer : Control, INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public MediaElement Player { get; set; }
    public Action RepeatEnded;
    public ObservableCollection<ICatTreeViewCategory> MediaItemClips { get; set; }
    public VideoClipViewModel CurrentVideoClip { get; set; }

    public string PositionSlashDuration {
      get {
        const string zeroTime = "00:00:00";
        if (Player == null) return zeroTime;
        var pos = new TimeSpan(0, 0, 0, (int) Math.Round(Player.Position.TotalSeconds), 0).ToString();
        var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan.ToString() : zeroTime;
        return $"{pos} / {dur}";
      }
    }

    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
      nameof(IsPlaying), typeof(bool), typeof(VideoPlayer));

    public bool IsPlaying {
      get => (bool) GetValue(IsPlayingProperty);
      set {
        SetValue(IsPlayingProperty, value);

        if (value) {
          Player.Play();
          _timelineTimer.Start();
          _clipTimer.Start();
        }
        else {
          _timelineTimer.Stop();
          _clipTimer.Stop();
          Player.Pause();
        }
      }
    }

    public static readonly DependencyProperty RotationProperty = DependencyProperty.Register(
      nameof(Rotation), typeof(double), typeof(VideoPlayer));

    public double Rotation {
      get => (double) GetValue(RotationProperty);
      set => SetValue(RotationProperty, value);
    }

    // 0 => infinity
    public static readonly DependencyProperty RepeatForSecondsProperty = DependencyProperty.Register(
      nameof(RepeatForSeconds), typeof(int), typeof(VideoPlayer), new PropertyMetadata(3));

    public int RepeatForSeconds {
      get => (int) GetValue(RepeatForSecondsProperty);
      set => SetValue(RepeatForSecondsProperty, value);
    }

    public static readonly DependencyProperty PlayTypeProperty = DependencyProperty.Register(
      nameof(PlayType), typeof(PlayType), typeof(VideoPlayer));

    public PlayType PlayType {
      get => (PlayType) GetValue(PlayTypeProperty);
      set {
        SetValue(PlayTypeProperty, value);

        if (value == PlayType.Clip && _ctvClips.SelectedItem is VideoClipViewModel vc) {
          _timelineSlider.Value = vc.Clip.TimeStart;
        }

        OnPropertyChanged(nameof(ShowRepeatSlider));
      }
    }

    public bool ShowRepeatSlider => PlayType == PlayType.Clips || PlayType == PlayType.Group;

    private CatTreeView _ctvClips;
    private Slider _timelineSlider;
    private Slider _speedSlider;
    private Slider _volumeSlider;
    private DispatcherTimer _timelineTimer;
    private DispatcherTimer _clipTimer;
    private bool _isTimelineTimerExecuting;
    private bool _wasPlaying;
    private int _repeatCount;

    static VideoPlayer() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(VideoPlayer), 
        new FrameworkPropertyMetadata(typeof(VideoPlayer)));
    }

    public void SetSource(MediaItem mediaItem) {
      if (mediaItem != null) {
        Rotation = mediaItem.RotationAngle;
        Player.Source = mediaItem.FilePathUri;
        IsPlaying = true;
      }
      else {
        Player.Source = null;
        IsPlaying = false;
      }
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      MediaItemClips = new ObservableCollection<ICatTreeViewCategory>();

      if (Template.FindName("PART_SpeedSlider", this) is Slider speedSlider) {
        _speedSlider = speedSlider;
        _speedSlider.ValueChanged += delegate { Player.SpeedRatio = _speedSlider.Value; };
      }

      if (Template.FindName("PART_VolumeSlider", this) is Slider volumeSlider) {
        _volumeSlider = volumeSlider;
        _volumeSlider.ValueChanged += delegate { Player.Volume = _volumeSlider.Value; };
      }

      if (Template.FindName("PART_MediaElement", this) is MediaElement mediaElement) {
        Player = mediaElement;
        Player.Volume = _volumeSlider.Value;
        Player.SpeedRatio = _speedSlider.Value;
        Player.MediaOpened += MediaElement_OnMediaOpened;
        Player.MediaEnded += MediaElement_OnMediaEnded;
        Player.MouseLeftButtonUp += MediaElement_OnMouseLeftButtonUp;
      }

      if (Template.FindName("PART_TimelineSlider", this) is Slider timelineSlider) {
        _timelineSlider = timelineSlider;
        _timelineSlider.ValueChanged += TimelineSlider_OnValueChanged;
        _timelineSlider.PreviewMouseLeftButtonUp += TimelineSlider_OnPreviewMouseLeftButtonUp;
        _timelineSlider.PreviewMouseLeftButtonDown += TimelineSlider_OnPreviewMouseLeftButtonDown;
      }

      if (Template.FindName("PART_ClipsPanel", this) is SlidePanel spClipsPanel)
        if (Template.FindName("PART_BtnIsPinned", this) is ToggleButton btnIsPinned) {
          BindingOperations.SetBinding(btnIsPinned, ToggleButton.IsCheckedProperty,
            new Binding(nameof(SlidePanel.IsPinned)) {Source = spClipsPanel});
        }

      if (Template.FindName("PART_BtnTimelineBeginning", this) is Button btnBeginning)
        btnBeginning.Click += ShiftTimelineButton_OnClick;

      if (Template.FindName("PART_BtnTimelineLargeBack", this) is Button btnLargeBack)
        btnLargeBack.Click += ShiftTimelineButton_OnClick;

      if (Template.FindName("PART_BtnTimelineSmallBack", this) is Button btnSmallBack)
        btnSmallBack.Click += ShiftTimelineButton_OnClick;

      if (Template.FindName("PART_BtnPlayPause", this) is Button btnPlayPause)
        btnPlayPause.Click += PlayPauseToggle;

      if (Template.FindName("PART_BtnTimelineSmallForward", this) is Button btnSmallForward)
        btnSmallForward.Click += ShiftTimelineButton_OnClick;

      if (Template.FindName("PART_BtnTimelineLargeForward", this) is Button btnLargeForward)
        btnLargeForward.Click += ShiftTimelineButton_OnClick;

      if (Template.FindName("PART_BtnTimelineEnd", this) is Button btnEnd)
        btnEnd.Click += ShiftTimelineButton_OnClick;

      if (Template.FindName("PART_BtnMarkerA", this) is Button btnMarkerA)
        btnMarkerA.Click += delegate {
          SetMarker(true);
        };

      if (Template.FindName("PART_BtnMarkerB", this) is Button btnMarkerB)
        btnMarkerB.Click += delegate {
          SetMarker(false);
        };

      if (Template.FindName("PART_SpPlayTypes", this) is StackPanel spPlayTypes)
        spPlayTypes.PreviewMouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs args) {
          if (args.OriginalSource is RadioButton rb) {
            PlayType = (PlayType) rb.Tag;
          }
        };

      if (Template.FindName("PART_CtvClips", this) is CatTreeView ctvClips) {
        _ctvClips = ctvClips;


        _ctvClips.SelectedItemChanged += delegate {
          CurrentVideoClip = _ctvClips.SelectedItem as VideoClipViewModel;
          if (CurrentVideoClip != null) {
            if (PlayType == PlayType.Clips || PlayType == PlayType.Group) {
              var d = (CurrentVideoClip.Clip.TimeEnd - CurrentVideoClip.Clip.TimeStart) / 1000.0;
              _repeatCount = (int)Math.Round(RepeatForSeconds / d, 0);
            }
            else {
              _repeatCount = 0;
            }
            
            _timelineSlider.Value = CurrentVideoClip.Clip.TimeStart;
            _volumeSlider.Value = CurrentVideoClip.Clip.Volume;
            _speedSlider.Value = CurrentVideoClip.Clip.Speed;
          }
        };

        _ctvClips.PreviewMouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs args) {
          if (args.OriginalSource is FrameworkElement fe && _ctvClips.SelectedItem is VideoClipViewModel vc) {
            switch (fe.Name) {
              // Seek to start video position defined in Clip
              case "TbMarkerA": {
                _timelineSlider.Value = vc.Clip.TimeStart;
                break;
              }
              // Seek to end video position defined in Clip
              case "TbMarkerB": {
                _timelineSlider.Value = vc.Clip.TimeEnd;
                break;
              }
            }
          }
        };
      }

      _timelineTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
      _timelineTimer.Tick += delegate {
        _isTimelineTimerExecuting = true;
        _timelineSlider.Value = Player.Position.TotalMilliseconds;
        _isTimelineTimerExecuting = false;
      };

      _clipTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
      _clipTimer.Tick += delegate {
        if (CurrentVideoClip == null || PlayType == PlayType.Video) return;

        var vc = CurrentVideoClip.Clip;

        if (vc.TimeEnd > vc.TimeStart && Player.Position.TotalMilliseconds > vc.TimeEnd) {
          switch (PlayType) {
            case PlayType.Clip:
              _timelineSlider.Value = vc.TimeStart;
              break;
            case PlayType.Clips:
            case PlayType.Group:
              if (_repeatCount > 0) {
                _repeatCount--;
                _timelineSlider.Value = vc.TimeStart;
              }
              else {
                App.Core.MediaItemClipsCategory.SelectNext(CurrentVideoClip, PlayType == PlayType.Group);
              }
              break;
          }
        }
      };

      MouseEnter += delegate (object sender, MouseEventArgs e) {
        var mec = (VideoPlayer)sender;
        mec.KeyDown += VideoPlayerControl_OnKeyDown;
        mec.Focus();
      };

      MouseLeave += delegate (object sender, MouseEventArgs e) {
        var mec = (VideoPlayer)sender;
        mec.KeyDown -= VideoPlayerControl_OnKeyDown;
      };
    }

    private void VideoPlayerControl_OnKeyDown(object sender, KeyEventArgs e) {
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
      if (!Player.HasVideo) return;

      var nd = Player.NaturalDuration;
      _repeatCount = (int)Math.Round(RepeatForSeconds / (nd.HasTimeSpan ? nd.TimeSpan.TotalMilliseconds / 1000.0 : 1), 0);

      _timelineSlider.Maximum = nd.HasTimeSpan ? nd.TimeSpan.TotalMilliseconds : 1000;
      IsPlaying = true;
    }

    private void MediaElement_OnMediaEnded(object sender, RoutedEventArgs e) {
      // if video doesn't have TimeSpan than is probably less than 1s long
      // and can't be repeated with MediaElement.Stop()/MediaElement.Play()
      Player.Position = TimeSpan.FromMilliseconds(1);

      if (_repeatCount > 0)
        _repeatCount--;
      else
        RepeatEnded();
    }

    private void MediaElement_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      PlayPauseToggle();
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

      Player.Position = new TimeSpan(0, 0, 0, 0, (int)e.NewValue);
    }

    private void ShiftTimelineButton_OnClick(object sender, RoutedEventArgs e) {
      ShiftTimeline((TimelineShift)((Button)sender).Tag);
    }

    private void ShiftTimeline(TimelineShift mode) {
      switch (mode) {
        case TimelineShift.Beginning:
          _timelineSlider.Value = _timelineSlider.Minimum;
          break;
        case TimelineShift.LargeBack:
          _timelineSlider.Value -= _timelineSlider.LargeChange;
          break;
        case TimelineShift.SmallBack:
          _timelineSlider.Value -= _timelineSlider.SmallChange;
          break;
        case TimelineShift.SmallForward:
          _timelineSlider.Value += _timelineSlider.SmallChange;
          break;
        case TimelineShift.LargeForward:
          _timelineSlider.Value += _timelineSlider.LargeChange;
          break;
        case TimelineShift.End:
          _timelineSlider.Value = _timelineSlider.Maximum;
          break;
      }
    }

    private void PlayPauseToggle() {
      IsPlaying = !IsPlaying;
    }

    private void PlayPauseToggle(object sender, RoutedEventArgs e) {
      PlayPauseToggle();
    }

    private void SetMarker(bool start) {
      if (!(_ctvClips.SelectedItem is VideoClipViewModel vc)) return;
      vc.SetMarker(start, Player.Position, _volumeSlider.Value, _speedSlider.Value);
      if (start) CreateThumbnail(vc, Player, true);
    }

    public void CreateThumbnail(VideoClipViewModel vc, FrameworkElement visual, bool reCreate = false) {
      if (!File.Exists(vc.ThumbPath.LocalPath) || reCreate) {
        Utils.Imaging.CreateVideoThumbnailFromVisual(visual, vc.ThumbPath.LocalPath, Settings.Default.ThumbnailSize);

        vc.OnPropertyChanged(nameof(vc.ThumbPath));
      }
    }
  }
}
