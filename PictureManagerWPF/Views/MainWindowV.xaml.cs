using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace PictureManager.Views {
  public partial class MainWindowV {
    private bool _isMouseHidden;
    private DispatcherTimer _mouseHideTimer;
    private TimeSpan _mouseHideTimerInterval = TimeSpan.FromMilliseconds(3000);
    private DateTime _mouseLastMove;

    public MainWindowV() {
      InitializeComponent();
      SetUpMouseHideTimer();
    }

    private void SetUpMouseHideTimer() {
      _mouseHideTimer = new() { Interval = _mouseHideTimerInterval };
      _mouseHideTimer.Tick += delegate {
        if (_isMouseHidden || !IsActive || DateTime.Now - _mouseLastMove < _mouseHideTimerInterval)
          return;
        
        _isMouseHidden = true;
        Mouse.OverrideCursor = Cursors.None;
      };

      MouseMove += delegate {
        _mouseLastMove = DateTime.Now;
        if (_isMouseHidden) {
          _isMouseHidden = false;
          Mouse.OverrideCursor = null;
        }
      };

      App.Core.MediaViewerM.PropertyChanged += (_, e) => {
        if (nameof(App.Core.MediaViewerM.IsVisible).Equals(e.PropertyName)) {
          if (App.Core.MediaViewerM.IsVisible)
            _mouseHideTimer.Start();
          else
            _mouseHideTimer.Stop();
        }
      };
    }
  }
}
