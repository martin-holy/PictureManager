using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Properties;
using PictureManager.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace PictureManager.CustomControls {
  public enum TimelineShift { Beginning, LargeBack, SmallBack, SmallForward, LargeForward, End }
  public enum PlayType { Video, Clip, Clips, Group }

  public class VideoPlayer : Control, INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(VideoPlayer));
    public static readonly DependencyProperty RotationProperty = DependencyProperty.Register(nameof(Rotation), typeof(double), typeof(VideoPlayer));
    public static readonly DependencyProperty RepeatForSecondsProperty = DependencyProperty.Register(nameof(RepeatForSeconds), typeof(int), typeof(VideoPlayer), new PropertyMetadata(3));
    public static readonly DependencyProperty PlayTypeProperty = DependencyProperty.Register(nameof(PlayType), typeof(PlayType), typeof(VideoPlayer));

    public MediaElement Player { get; set; }
    public Action RepeatEnded { get; set; }
    public ObservableCollection<ICatTreeViewCategory> MediaItemClips { get; set; }
    public VideoClipViewModel CurrentVideoClip { get; set; }
    public SlidePanel ClipsPanel { get; set; }
    public bool ShowRepeatSlider => PlayType is PlayType.Clips or PlayType.Group;
    public CatTreeView CtvClips { get; private set; }

    private Slider _timelineSlider;
    private Slider _speedSlider;
    private Slider _volumeSlider;
    private DispatcherTimer _timelineTimer;
    private readonly DispatcherTimer _mouseHideTimer;
    private DispatcherTimer _clipTimer;
    private DateTime _mouseLastMove;
    private bool _isMouseHidden;
    private bool _isTimelineTimerExecuting;
    private bool _wasPlaying;
    private int _repeatCount;
    private int _smallChange;

    public int SmallChange { get => _smallChange; set { _smallChange = value; OnPropertyChanged(); } }

    public double Rotation {
      get => (double)GetValue(RotationProperty);
      set => SetValue(RotationProperty, value);
    }

    // 0 => infinity
    public int RepeatForSeconds {
      get => (int)GetValue(RepeatForSecondsProperty);
      set => SetValue(RepeatForSecondsProperty, value);
    }

    public string PositionSlashDuration {
      get {
        const string zeroTime = "00:00:00";
        if (Player == null) return zeroTime;
        var pos = new TimeSpan(0, 0, 0, (int)Math.Round(Player.Position.TotalSeconds), 0).ToString();
        var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan.ToString() : zeroTime;
        return $"{pos} / {dur}";
      }
    }

    public bool IsPlaying {
      get => (bool)GetValue(IsPlayingProperty);
      set {
        SetValue(IsPlayingProperty, value);

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
      }
    }

    public PlayType PlayType {
      get => (PlayType)GetValue(PlayTypeProperty);
      set {
        SetValue(PlayTypeProperty, value);
        StartClipTimer();
        OnPropertyChanged();
        OnPropertyChanged(nameof(ShowRepeatSlider));
      }
    }

    static VideoPlayer() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(VideoPlayer), new FrameworkPropertyMetadata(typeof(VideoPlayer)));
    }

    public VideoPlayer() {
      MouseMove += (o, e) => {
        _mouseLastMove = DateTime.Now;
        if (_isMouseHidden) {
          _isMouseHidden = false;
          Cursor = Cursors.Arrow;
        }
      };

      _mouseHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(3000) };
      _mouseHideTimer.Tick += (o, e) => {
        if (_isMouseHidden || DateTime.Now - _mouseLastMove < _mouseHideTimer.Interval) return;
        _isMouseHidden = true;
        Cursor = Cursors.None;
      };
    }

    public void SetSource(MediaItem mediaItem) {
      if (mediaItem != null) {
        var data = ShellStuff.FileInformation.GetVideoMetadata(mediaItem.Folder.FullPath, mediaItem.FileName);
        var fps = (double)data[3] > 0 ? (double)data[3] : 30.0;
        SmallChange = (int)Math.Round(1000 / fps, 0);
        Rotation = mediaItem.RotationAngle;
        Player.Source = mediaItem.FilePathUri;
        IsPlaying = true;
      }
      else {
        Player.Source = null;
        IsPlaying = false;
      }
    }

    private void StartClipTimer() {
      _clipTimer.Stop();

      if (CurrentVideoClip == null || PlayType == PlayType.Video) return;

      var vc = CurrentVideoClip.Clip;
      var duration = (vc.TimeEnd - vc.TimeStart) / _speedSlider.Value;

      if (duration <= 0) return;

      _repeatCount = PlayType is PlayType.Clips or PlayType.Group ? (int)Math.Round(RepeatForSeconds / (duration / 1000.0), 0) : 0;
      _timelineSlider.Value = vc.TimeStart;
      _clipTimer.Interval = TimeSpan.FromMilliseconds(duration);
      _clipTimer.Start();
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      MediaItemClips = new ObservableCollection<ICatTreeViewCategory>();

      if (Template.FindName("PART_SpeedSlider", this) is Slider speedSlider) {
        _speedSlider = speedSlider;
        _speedSlider.ValueChanged += (o, e) => {
          Player.SpeedRatio = _speedSlider.Value;
          StartClipTimer();
        };
      }

      if (Template.FindName("PART_VolumeSlider", this) is Slider volumeSlider) {
        _volumeSlider = volumeSlider;
        _volumeSlider.ValueChanged += (o, e) => { Player.Volume = _volumeSlider.Value; };
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

      if (Template.FindName("PART_ClipsPanel", this) is SlidePanel spClipsPanel) {
        if (Template.FindName("PART_BtnIsPinned", this) is ToggleButton btnIsPinned) {
          BindingOperations.SetBinding(btnIsPinned, ToggleButton.IsCheckedProperty,
            new Binding(nameof(SlidePanel.IsPinned)) { Source = spClipsPanel });
        }

        ClipsPanel = spClipsPanel;
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
        btnMarkerA.Click += (o, e) => SetMarker(CurrentVideoClip, true);

      if (Template.FindName("PART_BtnMarkerB", this) is Button btnMarkerB)
        btnMarkerB.Click += (o, e) => SetMarker(CurrentVideoClip, false);

      if (Template.FindName("PART_SpPlayTypes", this) is StackPanel spPlayTypes)
        spPlayTypes.PreviewMouseLeftButtonUp += (o, e) => {
          if (e.OriginalSource is RadioButton rb) {
            PlayType = (PlayType)rb.Tag;
          }
        };

      if (Template.FindName("PART_CtvClips", this) is CatTreeView ctvClips) {
        CtvClips = ctvClips;

        CtvClips.SelectedItemChanged += (o, e) => {
          CurrentVideoClip = CtvClips.SelectedItem as VideoClipViewModel;
          if (CurrentVideoClip != null && PlayType != PlayType.Video) {
            _volumeSlider.Value = CurrentVideoClip.Clip.Volume;
            _speedSlider.Value = CurrentVideoClip.Clip.Speed;
          }

          if (IsPlaying) StartClipTimer();
        };

        CtvClips.PreviewMouseLeftButtonUp += (o, e) => {
          if (e.OriginalSource is FrameworkElement fe && CtvClips.SelectedItem is VideoClipViewModel vc) {
            switch (fe.Name) {
              case "TbMarkerA": {
                // Seek to start video position defined in Clip
                _timelineSlider.Value = vc.Clip.TimeStart;
                break;
              }
              case "TbMarkerB": {
                // Seek to end video position defined in Clip
                _timelineSlider.Value = vc.Clip.TimeEnd;
                break;
              }
            };
          }
        };
      }

      _timelineTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
      _timelineTimer.Tick += (o, e) => {
        _isTimelineTimerExecuting = true;
        _timelineSlider.Value = Math.Round(Player.Position.TotalMilliseconds / SmallChange) * SmallChange;
        _isTimelineTimerExecuting = false;
      };

      _clipTimer = new() { Interval = TimeSpan.FromMilliseconds(10) };
      _clipTimer.Tick += (o, e) => {
        if (CurrentVideoClip == null || PlayType == PlayType.Video) return;

        var vc = CurrentVideoClip.Clip;
        if (vc.TimeEnd > vc.TimeStart) {
          switch (PlayType) {
            case PlayType.Clip: {
              _timelineSlider.Value = vc.TimeStart;
              break;
            }
            case PlayType.Clips:
            case PlayType.Group: {
              if (_repeatCount > 0) {
                _repeatCount--;
                _timelineSlider.Value = vc.TimeStart;
              }
              else {
                App.Ui.MediaItemClipsCategory.SelectNext(CurrentVideoClip, PlayType == PlayType.Group);
              }
              break;
            }
          }
        }
      };

      MouseEnter += (o, e) => {
        var mec = (VideoPlayer)o;
        mec.KeyDown += VideoPlayerControl_OnKeyDown;
        mec.Focus();
      };

      MouseLeave += (o, e) => {
        var mec = (VideoPlayer)o;
        mec.KeyDown -= VideoPlayerControl_OnKeyDown;
      };
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
      _timelineSlider.Maximum = nd.HasTimeSpan ? nd.TimeSpan.TotalMilliseconds : 1000;
      IsPlaying = true;

      if (PlayType != PlayType.Video)
        App.Ui.MediaItemClipsCategory.SelectNext(null, false);
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

    private void MediaElement_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => PlayPauseToggle();

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

    private void ShiftTimelineButton_OnClick(object sender, RoutedEventArgs e) =>
      ShiftTimeline((TimelineShift)((Button)sender).Tag);

    private void ShiftTimeline(TimelineShift mode) {
      _timelineSlider.Value = mode switch {
        TimelineShift.Beginning => _timelineSlider.Minimum,
        TimelineShift.LargeBack => _timelineSlider.Value - _timelineSlider.LargeChange,
        TimelineShift.SmallBack => _timelineSlider.Value - _timelineSlider.SmallChange,
        TimelineShift.SmallForward => _timelineSlider.Value + _timelineSlider.SmallChange,
        TimelineShift.LargeForward => _timelineSlider.Value + _timelineSlider.LargeChange,
        TimelineShift.End => _timelineSlider.Maximum,
        _ => throw new NotImplementedException()
      };
    }

    private void PlayPauseToggle() => IsPlaying = !IsPlaying;

    private void PlayPauseToggle(object sender, RoutedEventArgs e) => PlayPauseToggle();

    public void SetMarker(VideoClipViewModel vc, bool start) {
      vc.SetMarker(start, (int)Math.Round(_timelineSlider.Value), _volumeSlider.Value, _speedSlider.Value);
      if (start) CreateThumbnail(vc, Player, true);
    }

    public static void CreateThumbnail(VideoClipViewModel vc, FrameworkElement visual, bool reCreate = false) {
      if (!File.Exists(vc.ThumbPath.LocalPath) || reCreate) {
        Imaging.CreateVideoThumbnailFromVisual(visual, vc.ThumbPath.LocalPath, Settings.Default.ThumbnailSize, Settings.Default.JpegQualityLevel);

        vc.OnPropertyChanged(nameof(vc.ThumbPath));
      }
    }
  }
}
