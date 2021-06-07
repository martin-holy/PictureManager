using System;
using System.Timers;
using System.Windows;
using PictureManager.CustomControls;
using PictureManager.Domain;

namespace PictureManager.UserControls {
  public partial class PresentationPanel {
    public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
      nameof(IsRunning), typeof(bool), typeof(PresentationPanel));
    
    public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
      nameof(Interval), typeof(int), typeof(PresentationPanel), new UIPropertyMetadata(3));

    public static readonly DependencyProperty PlayPanoramicImagesProperty = DependencyProperty.Register(
      nameof(PlayPanoramicImages), typeof(bool), typeof(PresentationPanel), new UIPropertyMetadata(true));

    public bool IsRunning {
      get => (bool) GetValue(IsRunningProperty);
      set {
        SetValue(IsRunningProperty, value);
        _timer.Enabled = value;
      }
    }

    public int Interval {
      get => (int) GetValue(IntervalProperty);
      set {
        SetValue(IntervalProperty, value);
        _timer.Interval = value * 1000;
      }
    }

    public bool PlayPanoramicImages {
      get => (bool) GetValue(PlayPanoramicImagesProperty);
      set => SetValue(PlayPanoramicImagesProperty, value);
    }

    public PresentationPanel() {
      _timer = new Timer();
      _timer.Elapsed += (o, e) => {
        Elapsed();
      };

      InitializeComponent();
    }

    private readonly Timer _timer;

    public bool IsPaused { get; private set; }
    
    public Action Elapsed;

    ~PresentationPanel() {
      _timer?.Dispose();
    }

    public void Start(bool delay) {
      if (App.Ui.AppInfo.AppMode != AppMode.Viewer) return;

      var current = App.Core.MediaItems.ThumbsGrid.Current;
      if (delay && current.MediaType == MediaType.Image && current.IsPanoramic && PlayPanoramicImages) {
        Pause();
        App.WMain.FullImage.Play(Interval * 1000, delegate { Start(false); });
        return;
      }

      IsPaused = false;
      IsRunning = true;
      App.WMain.FullMedia.PlayType = PlayType.Video;
      App.WMain.FullMedia.RepeatForSeconds = Interval;

      if (!delay) Elapsed();
    }

    public void Stop() {
      IsPaused = false;
      IsRunning = false;
      App.WMain.FullMedia.RepeatForSeconds = 0; // infinity
    }

    public void Pause() {
      IsPaused = true;
      IsRunning = false;
    }

    private void ChangeInterval(object sender, RoutedPropertyChangedEventArgs<double> e) {
      Interval = (int) IntervalSlider.Value;
    }
  }
}
