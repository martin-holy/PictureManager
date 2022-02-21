using System.Timers;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain;

namespace PictureManager.ViewModels {
  public sealed class PresentationPanelVM : ObservableObject {
    private bool _isRunning;
    private bool _playPanoramicImages = true;
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

    public int Interval {
      get => _interval;
      set {
        _interval = value;
        _timer.Interval = value * 1000;
        OnPropertyChanged();
      }
    }

    public bool IsPaused { get; private set; }
    public RelayCommand<object> PresentationCommand { get; set; }

    public PresentationPanelVM(MediaViewerVM mediaViewerVM) {
      _mediaViewerVM = mediaViewerVM;
      PresentationCommand = new(Presentation);

      _timer.Interval = Interval * 1000;
      _timer.Elapsed += (_, _) => Next();
    }

    ~PresentationPanelVM() {
      _timer?.Dispose();
    }

    public void Start(bool delay) {
      if (delay && _mediaViewerVM.Current.MediaType == MediaType.Image && _mediaViewerVM.Current.IsPanoramic && PlayPanoramicImages) {
        Pause();
        _mediaViewerVM.FullImage.Play(Interval * 1000, () => Start(false));
        return;
      }

      IsPaused = false;
      IsRunning = true;
      _mediaViewerVM.FullVideo.PlayType = PlayType.Video;
      _mediaViewerVM.FullVideo.RepeatForSeconds = Interval;

      if (!delay) Next();
    }

    public void Stop() {
      IsPaused = false;
      IsRunning = false;
      _mediaViewerVM.FullVideo.RepeatForSeconds = 0; // infinity
    }

    public void Pause() {
      IsPaused = true;
      IsRunning = false;
    }

    private void Presentation() {
      if (_mediaViewerVM.FullImage.IsAnimationOn) {
        _mediaViewerVM.FullImage.Stop();
        Stop();
        return;
      }

      if (IsRunning || IsPaused)
        Stop();
      else
        Start(true);
    }

    private void Next() {
      Core.RunOnUiThread(() => {
        if (IsPaused) return;
        if (_mediaViewerVM.CanNext())
          _mediaViewerVM.Next();
        else
          Stop();
      });
    }
  }
}
