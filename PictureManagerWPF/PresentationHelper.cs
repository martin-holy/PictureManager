using System;
using System.Timers;

namespace PictureManager {
  public class PresentationHelper {
    private const int PresentationInterval = 3000;
    private readonly Timer _timer;

    public bool IsPaused { get; private set; }
    public bool IsEnabled => _timer.Enabled;
    public Action Elapsed;

    public PresentationHelper() {
      _timer = new Timer();
      _timer.Elapsed += (o, e) => {
        if (_timer.Interval == 1)
          _timer.Interval = PresentationInterval;

        Elapsed();
      };
    }

    ~PresentationHelper() {
      _timer?.Dispose();
    }

    public void Start(bool delay) {
      if (App.Core.AppInfo.AppMode != AppMode.Viewer) return;

      var current = App.Core.MediaItems.Current;
      if (delay && current.MediaType == MediaType.Image && current.IsPanoramatic) {
        Pause();
        App.WMain.FullImage.Play(PresentationInterval, delegate { Start(false); });
        return;
      }

      IsPaused = false;
      _timer.Interval = delay ? PresentationInterval : 1;
      _timer.Enabled = true;
      App.WMain.FullMedia.RepeatForMilliseconds = PresentationInterval;
    }

    public void Stop() {
      IsPaused = false;
      _timer.Enabled = false;
      App.WMain.FullMedia.RepeatForMilliseconds = 0; // infinity
    }

    public void Pause() {
      IsPaused = true;
      _timer.Enabled = false;
    }
  }
}
