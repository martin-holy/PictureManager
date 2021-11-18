using PictureManager.Commands;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PictureManager.Domain.Models;
using PictureManager.ViewModels.Tree;

namespace PictureManager.CustomControls {
  public enum TimelineShift { Beginning, LargeBack, SmallBack, SmallForward, LargeForward, End }
  public enum PlayType { Video, Clip, Clips, Group }

  public class VideoPlayer : Control, INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    private void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged.Invoke(this, new(name));

    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(nameof(IsPlaying), typeof(bool), typeof(VideoPlayer));
    public static readonly DependencyProperty RotationProperty = DependencyProperty.Register(nameof(Rotation), typeof(double), typeof(VideoPlayer));
    public static readonly DependencyProperty RepeatForSecondsProperty = DependencyProperty.Register(nameof(RepeatForSeconds), typeof(int), typeof(VideoPlayer), new(3));
    public static readonly DependencyProperty PlayTypeProperty = DependencyProperty.Register(nameof(PlayType), typeof(PlayType), typeof(VideoPlayer), new(PlayType.Video, OnPlayTypeChanged));

    //public static RoutedUICommand VideoClipSplitCommand { get; } = CommandsController.CreateCommand("Split", "Split", new KeyGesture(Key.S, ModifierKeys.Alt));
    public static RoutedUICommand VideoClipsSaveCommand { get; } = new() { Text = "Save Video Clips" };

    public MediaElement Player { get; set; }
    public Action RepeatEnded { get; set; }
    public VideoClipTreeVM CurrentVideoClip { get; set; }
    public bool ShowRepeatSlider => PlayType is PlayType.Clips or PlayType.Group;
    public Slider TimelineSlider { get; private set; }
    public Slider SpeedSlider { get; private set; }
    public Slider VolumeSlider { get; private set; }

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
      set => SetValue(PlayTypeProperty, value);
    }

    private static void OnPlayTypeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
      var vp = (VideoPlayer)o;
      vp.StartClipTimer();
      vp.OnPropertyChanged(nameof(ShowRepeatSlider));
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

      Loaded += (o, e) => SetUpCommands(App.WMain.CommandBindings);
    }

    private void SetUpCommands(CommandBindingCollection cbc) {
      MediaCommands.TogglePlayPause.InputGestures.Add(new KeyGesture(Key.Space));
      MediaCommands.TogglePlayPause.InputGestures.Add(new MouseGesture(MouseAction.LeftClick));
      CommandsController.AddCommandBinding(cbc, VideoClipsSaveCommand, VideoClipsSave, CanVideoClipsSave);
      CommandsController.SetTargetToCommand(MediaCommands.TogglePlayPause, this);
    }

    public void SetSource(MediaItemM mediaItem) {
      if (mediaItem != null) {
        var data = ShellStuff.FileInformation.GetVideoMetadata(mediaItem.Folder.FullPath, mediaItem.FileName);
        var fps = (double)data[3] > 0 ? (double)data[3] : 30.0;
        SmallChange = (int)Math.Round(1000 / fps, 0);
        Rotation = mediaItem.RotationAngle;
        Player.Source = new(mediaItem.FilePath);
        IsPlaying = true;
      }
      else {
        Player.Source = null;
        IsPlaying = false;
      }

      App.WMain.ToolsTabs.Activate(App.WMain.ToolsTabs.TabClips, mediaItem != null);
    }

    public void StartClipTimer() {
      _clipTimer.Stop();

      if (CurrentVideoClip == null || PlayType == PlayType.Video) return;

      var vc = CurrentVideoClip.Model;
      var duration = (vc.TimeEnd - vc.TimeStart) / SpeedSlider.Value;

      if (duration <= 0) return;

      _repeatCount = PlayType is PlayType.Clips or PlayType.Group ? (int)Math.Round(RepeatForSeconds / (duration / 1000.0), 0) : 0;
      TimelineSlider.Value = vc.TimeStart;
      _clipTimer.Interval = TimeSpan.FromMilliseconds(duration);
      _clipTimer.Start();
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      if (Template.FindName("PART_SpeedSlider", this) is Slider speedSlider) {
        SpeedSlider = speedSlider;
        SpeedSlider.ValueChanged += (o, e) => {
          Player.SpeedRatio = SpeedSlider.Value;
          StartClipTimer();
        };
      }

      if (Template.FindName("PART_VolumeSlider", this) is Slider volumeSlider) {
        VolumeSlider = volumeSlider;
        VolumeSlider.ValueChanged += (o, e) => Player.Volume = VolumeSlider.Value;
      }

      if (Template.FindName("PART_MediaElement", this) is MediaElement mediaElement) {
        Player = mediaElement;
        Player.Volume = VolumeSlider.Value;
        Player.SpeedRatio = SpeedSlider.Value;
        Player.MediaOpened += MediaElement_OnMediaOpened;
        Player.MediaEnded += MediaElement_OnMediaEnded;
        Player.MouseLeftButtonUp += MediaElement_OnMouseLeftButtonUp;
      }

      if (Template.FindName("PART_TimelineSlider", this) is Slider timelineSlider) {
        TimelineSlider = timelineSlider;
        TimelineSlider.ValueChanged += TimelineSlider_OnValueChanged;
        TimelineSlider.PreviewMouseLeftButtonUp += TimelineSlider_OnPreviewMouseLeftButtonUp;
        TimelineSlider.PreviewMouseLeftButtonDown += TimelineSlider_OnPreviewMouseLeftButtonDown;
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

      _timelineTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
      _timelineTimer.Tick += (o, e) => {
        _isTimelineTimerExecuting = true;
        TimelineSlider.Value = Math.Round(Player.Position.TotalMilliseconds / SmallChange) * SmallChange;
        _isTimelineTimerExecuting = false;
      };

      _clipTimer = new() { Interval = TimeSpan.FromMilliseconds(10) };
      _clipTimer.Tick += (o, e) => {
        if (CurrentVideoClip == null || PlayType == PlayType.Video) return;

        var vc = CurrentVideoClip.Model;
        if (vc.TimeEnd > vc.TimeStart) {
          switch (PlayType) {
            case PlayType.Clip:
            TimelineSlider.Value = vc.TimeStart;
            break;

            case PlayType.Clips:
            case PlayType.Group:
            if (_repeatCount > 0) {
              _repeatCount--;
              TimelineSlider.Value = vc.TimeStart;
            }
            else {
              App.Ui.VideoClipsTreeVM.SelectNext(CurrentVideoClip, PlayType == PlayType.Group);
            }
            break;
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
      TimelineSlider.Maximum = nd.HasTimeSpan ? nd.TimeSpan.TotalMilliseconds : 1000;
      IsPlaying = true;

      if (PlayType != PlayType.Video)
        App.Ui.VideoClipsTreeVM.SelectNext(null, false);
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
      TimelineSlider.Value = mode switch {
        TimelineShift.Beginning => TimelineSlider.Minimum,
        TimelineShift.LargeBack => TimelineSlider.Value - TimelineSlider.LargeChange,
        TimelineShift.SmallBack => TimelineSlider.Value - TimelineSlider.SmallChange,
        TimelineShift.SmallForward => TimelineSlider.Value + TimelineSlider.SmallChange,
        TimelineShift.LargeForward => TimelineSlider.Value + TimelineSlider.LargeChange,
        TimelineShift.End => TimelineSlider.Maximum,
        _ => throw new NotImplementedException()
      };
    }

    private void PlayPauseToggle() => IsPlaying = !IsPlaying;

    private void PlayPauseToggle(object sender, RoutedEventArgs e) => PlayPauseToggle();

    public void SetMarker(VideoClipTreeVM vc, bool start) {
      App.Core.VideoClipsM.SetMarker(vc.Model, start, (int)Math.Round(TimelineSlider.Value), VolumeSlider.Value, SpeedSlider.Value);
      if (start) VideoClipsTreeVM.CreateThumbnail(vc.Model, Player, true);
    }

    private static bool CanVideoClipsSave() =>
      App.Core.VideoClipsM.DataAdapter.IsModified || App.Core.VideoClipsGroupsM.DataAdapter.IsModified;

    private static void VideoClipsSave() {
      App.Core.VideoClipsM.DataAdapter.Save();
      App.Core.VideoClipsGroupsM.DataAdapter.Save();
    }
  }
}
