using System.Windows;
using System.Windows.Controls;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.ViewModels;

namespace PictureManager.CustomControls {
  public enum TimelineShift { Beginning, LargeBack, SmallBack, SmallForward, LargeForward, End }
  public enum PlayType { Video, Clip, Clips }

  public class VideoPlayer : Control, INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public MediaElement MediaElement { get; set; }
    public Action RepeatEnded;
    public ObservableCollection<VideoClipViewModel> Clips { get; set; }
    public VideoClipViewModel CurrentVideoClip { get; set; }

    public string PositionSlashDuration {
      get {
        const string zeroTime = "00:00:00";
        if (MediaElement == null) return zeroTime;
        var pos = new TimeSpan(0, 0, 0, (int)Math.Round(MediaElement.Position.TotalSeconds), 0).ToString();
        var dur = MediaElement.NaturalDuration.HasTimeSpan ? MediaElement.NaturalDuration.TimeSpan.ToString() : zeroTime;
        return $"{pos} / {dur}";
      }
    }

    public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register(
      nameof(IsPlaying), typeof(bool), typeof(VideoPlayer));

    public bool IsPlaying {
      get => (bool)GetValue(IsPlayingProperty);
      set {
        SetValue(IsPlayingProperty, value);

        if (value) {
          MediaElement.Play();
          _timelineTimer.Start();
          _clipTimer.Start();
        }
        else {
          _timelineTimer.Stop();
          _clipTimer.Stop();
          MediaElement.Pause();
        }
      }
    }

    public static readonly DependencyProperty RotationProperty = DependencyProperty.Register(
      nameof(Rotation), typeof(double), typeof(VideoPlayer));

    public double Rotation {
      get => (double)GetValue(RotationProperty);
      set => SetValue(RotationProperty, value);
    }

    // 0 => infinity
    public static readonly DependencyProperty RepeatForSecondsProperty = DependencyProperty.Register(
      nameof(RepeatForSeconds), typeof(int), typeof(VideoPlayer), new PropertyMetadata(3));

    public int RepeatForSeconds {
      get => (int)GetValue(RepeatForSecondsProperty);
      set => SetValue(RepeatForSecondsProperty, value);
    }

    public static readonly DependencyProperty PlayTypeProperty = DependencyProperty.Register(
      nameof(PlayType), typeof(PlayType), typeof(VideoPlayer));

    public PlayType PlayType {
      get => (PlayType)GetValue(PlayTypeProperty);
      set {
        SetValue(PlayTypeProperty, value);

        if (value == PlayType.Clip && _tvClips.SelectedItem is VideoClipViewModel vc) {
          _timelineSlider.Value = vc.Clip.TimeStart;
        }
      }
    }

    private TreeView _tvClips;
    private Slider _timelineSlider;
    private Slider _speedSlider;
    private Slider _volumeSlider;
    private DispatcherTimer _timelineTimer;
    private DispatcherTimer _clipTimer;
    private bool _isTimelineTimerExecuting;
    private bool _wasPlaying;
    private int _repeatCount;
    private MediaItem _mediaItem;

    static VideoPlayer() {
      DefaultStyleKeyProperty.OverrideMetadata(typeof(VideoPlayer), 
        new FrameworkPropertyMetadata(typeof(VideoPlayer)));
    }

    public void SetSource(MediaItem mediaItem) {
      if (mediaItem != null) {
        Clips.Clear();
        Rotation = mediaItem.RotationAngle;
        MediaElement.Source = mediaItem.FilePathUri;
        _mediaItem = mediaItem;

        if (mediaItem.VideoClips != null) {
          IsPlaying = false;

          var i = 0;
          foreach (var vc in mediaItem.VideoClips) {
            i++;
            var vcvm = new VideoClipViewModel(vc) {Index = i};
            vcvm.CreateThumbnail(MediaElement);
            Clips.Add(vcvm);
          }

          MediaElement.Position = TimeSpan.FromMilliseconds(1);
        }

        IsPlaying = true;
      }
      else {
        MediaElement.Source = null;
        IsPlaying = false;
        Clips.Clear();
        _mediaItem = null;
      }
    }

    public void SplitClip() {
      if (_tvClips.SelectedItem is VideoClipViewModel vc && vc.Clip.TimeEnd == 0)
        SetMarker(vc, false);
      else
        AddClip();
    }

    private async void AddClip() {
      var vc = await Core.Instance.VideoClips.CreateVideoClipAsync(_mediaItem, _volumeSlider.Value, _speedSlider.Value);
      var vcvm = new VideoClipViewModel(vc) {Index = Clips.Count + 1};
      Clips.Add(vcvm);
      SetMarker(vcvm, true);
      vcvm.IsSelected = true;
    }

    public override void OnApplyTemplate() {
      base.OnApplyTemplate();

      Clips = new ObservableCollection<VideoClipViewModel>();

      if (Template.FindName("PART_SpeedSlider", this) is Slider speedSlider) {
        _speedSlider = speedSlider;
        _speedSlider.ValueChanged += ChangeMediaSpeed;
      }

      if (Template.FindName("PART_VolumeSlider", this) is Slider volumeSlider) {
        _volumeSlider = volumeSlider;
        _volumeSlider.ValueChanged += ChangeMediaVolume;
      }

      if (Template.FindName("PART_MediaElement", this) is MediaElement mediaElement) {
        MediaElement = mediaElement;
        MediaElement.Volume = _volumeSlider.Value;
        MediaElement.SpeedRatio = _speedSlider.Value;
        MediaElement.MediaOpened += MediaElement_OnMediaOpened;
        MediaElement.MediaEnded += MediaElement_OnMediaEnded;
        MediaElement.MouseLeftButtonUp += MediaElement_OnMouseLeftButtonUp;
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
          SetMarker(_tvClips.SelectedItem as VideoClipViewModel, true);
        };

      if (Template.FindName("PART_BtnMarkerB", this) is Button btnMarkerB)
        btnMarkerB.Click += delegate {
          SetMarker(_tvClips.SelectedItem as VideoClipViewModel, false);
        };

      if (Template.FindName("PART_BtnAddClip", this) is Button btnAddClip)
        btnAddClip.Click += delegate {
          AddClip();
        };

      if (Template.FindName("PART_BtnRemoveClip", this) is Button btnRemoveClip)
        btnRemoveClip.Click += delegate {
          if (_tvClips.SelectedItem is VideoClipViewModel vc) {
            if (!MessageDialog.Show("Delete Confirmation",$"Do you really want to delete {vc.Name}?", true)) return;
            Core.Instance.VideoClips.ItemDelete(vc.Clip);
            File.Delete(vc.ThumbPath.LocalPath);
            Clips.Remove(vc);
            for (var i = 0; i < Clips.OfType<VideoClipViewModel>().Count(); i++)
              ((VideoClipViewModel) Clips[i]).Index = i + 1;
          }
        };

      if (Template.FindName("PART_SpPlayTypes", this) is StackPanel spPlayTypes)
        spPlayTypes.PreviewMouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs args) {
          if (args.OriginalSource is RadioButton rb) {
            PlayType = (PlayType) rb.Tag;
          }
        };

      if (Template.FindName("PART_TvClips", this) is TreeView tvClips) {
        _tvClips = tvClips;

        _tvClips.SelectedItemChanged += delegate {
          CurrentVideoClip = _tvClips.SelectedItem as VideoClipViewModel;
          if (CurrentVideoClip != null) {
            if (PlayType == PlayType.Clips) {
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

        _tvClips.PreviewMouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs args) {
          if (args.OriginalSource is FrameworkElement fe && _tvClips.SelectedItem is VideoClipViewModel vc) {
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
              // Rename Clip
              case "TbName": {
                var result = InputDialog.Open(
                  IconName.Notification,
                  "Rename Clip",
                  "Enter new Clip name",
                  vc.Clip.Name,
                  answer => null,
                  out var output);

                if (!result) return;
                vc.RenameClip(output);
                  break;
              }
            }
          }
        };
      }

      _timelineTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
      _timelineTimer.Tick += delegate {
        _isTimelineTimerExecuting = true;
        _timelineSlider.Value = MediaElement.Position.TotalMilliseconds;
        _isTimelineTimerExecuting = false;
      };

      _clipTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
      _clipTimer.Tick += delegate {
        if (CurrentVideoClip == null) return;

        var vc = CurrentVideoClip.Clip;

        if (vc.TimeEnd > vc.TimeStart && MediaElement.Position.TotalMilliseconds > vc.TimeEnd) {
          if (PlayType == PlayType.Clip)
            _timelineSlider.Value = vc.TimeStart;
          
          if (PlayType == PlayType.Clips) {
            if (_repeatCount > 0) {
              _repeatCount--;
              _timelineSlider.Value = vc.TimeStart;
            }
            else {
              var i = Clips.IndexOf(CurrentVideoClip);
              if (i < Clips.Count - 1)
                Clips[i + 1].IsSelected = true;
              else
                Clips[0].IsSelected = true;
            }
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

    private void SetMarker(VideoClipViewModel vc, bool start) {
      if (vc == null) return;
      vc.SetMarker(start, MediaElement.Position, _volumeSlider.Value, _speedSlider.Value);
      if (start)
        vc.CreateThumbnail(MediaElement, true);
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
      if (!MediaElement.HasVideo) return;

      var nd = MediaElement.NaturalDuration;
      _repeatCount = (int)Math.Round(RepeatForSeconds / (nd.HasTimeSpan ? nd.TimeSpan.TotalMilliseconds / 1000 : 1), 0);

      _timelineSlider.Maximum = nd.HasTimeSpan ? nd.TimeSpan.TotalMilliseconds : 1000;
      IsPlaying = true;
    }

    private void MediaElement_OnMediaEnded(object sender, RoutedEventArgs e) {
      if (_repeatCount > 0 || PlayType != PlayType.Clips) {
        _repeatCount--;

        // if video doesn't have TimeSpan than is probably less than 1s long
        // and can't be repeated with MediaElement.Stop()/MediaElement.Play()
        MediaElement.Position = TimeSpan.FromMilliseconds(1);

        return;
      }

      RepeatEnded();
    }

    private void MediaElement_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      PlayPauseToggle();
    }

    private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> e) {
      MediaElement.Volume = _volumeSlider.Value;
    }

    private void ChangeMediaSpeed(object sender, RoutedPropertyChangedEventArgs<double> e) {
      MediaElement.SpeedRatio = _speedSlider.Value;
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


  }
}
