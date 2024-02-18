using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.ViewModels;
using System.Timers;

namespace PictureManager.Domain.Models {
    public sealed class PresentationPanelM : ObservableObject {
    private bool _isRunning;
    private bool _playPanoramicImages = true;
    private bool _isAnimationOn;
    private int _minAnimationDuration;
    private int _interval = 3;
    private readonly Timer _timer = new();
    private readonly MediaViewerVM _mediaViewerVM;

    public bool IsRunning {
      get => _isRunning;
      set {
        _isRunning = value;
        _timer.Enabled = value;
        OnPropertyChanged();
      }
    }

    public bool PlayPanoramicImages {
      get => _playPanoramicImages;
      set {
        _playPanoramicImages = value;
        OnPropertyChanged();
      }
    }

    public bool IsAnimationOn {
      get => _isAnimationOn;
      set {
        _isAnimationOn = value;
        if (!value)
          Start(_mediaViewerVM.Current, false);
        OnPropertyChanged();
      }
    }

    public int MinAnimationDuration { get => _minAnimationDuration; set { _minAnimationDuration = value; OnPropertyChanged(); } }

    public int Interval {
      get => _interval;
      set {
        _interval = value;
        _timer.Interval = value * 1000;
        OnPropertyChanged();
      }
    }

    public bool IsPaused { get; private set; }
    public static RelayCommand StartPresentationCommand { get; set; }
    public static RelayCommand StopPresentationCommand { get; set; }

    public PresentationPanelM(MediaViewerVM mediaViewerVM) {
      _mediaViewerVM = mediaViewerVM;
      StartPresentationCommand = new(Presentation, MH.Utils.Res.IconPlay, "Start presentation");
      StopPresentationCommand = new(Presentation, MH.Utils.Res.IconStop, "Stop presentation");

      _timer.Interval = Interval * 1000;
      _timer.Elapsed += (_, _) => Next();
    }

    ~PresentationPanelM() {
      _timer?.Dispose();
    }

    public void Start(MediaItemM current, bool delay) {
      if (delay
        && PlayPanoramicImages
        && current is ImageM 
        && current.IsPanoramic()) {
        Pause();
        MinAnimationDuration = Interval * 1000;
        IsAnimationOn = true;
        return;
      }

      IsPaused = false;
      IsRunning = true;
      Core.VideoDetail.MediaPlayer.PlayType = PlayType.Video;
      Core.VideoDetail.MediaPlayer.RepeatForSeconds = Interval;

      if (!delay) Next();
    }

    public void Stop() {
      if (IsAnimationOn)
        IsAnimationOn = false;

      IsPaused = false;
      IsRunning = false;
      Core.VideoDetail.MediaPlayer.RepeatForSeconds = 0; // infinity
    }

    public void Pause() {
      IsPaused = true;
      IsRunning = false;
    }

    private void Presentation() {
      if (IsAnimationOn || IsRunning || IsPaused)
        Stop();
      else
        Start(_mediaViewerVM.Current, true);
    }

    private void Next() {
      Tasks.RunOnUiThread(() => {
        if (IsPaused) return;
        if (_mediaViewerVM.CanNext())
          _mediaViewerVM.Next();
        else
          Stop();
      });
    }

    public void Next(MediaItemM current) {
      // TODO MediaTypes
      if (IsRunning && (current is VideoM || (current.IsPanoramic() && PlayPanoramicImages))) {
        Pause();

        if (current is ImageM)
          Start(current, true);
      }
    }
  }
}
