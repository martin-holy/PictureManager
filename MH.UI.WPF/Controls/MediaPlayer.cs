using MH.UI.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UIC = MH.UI.Controls;

namespace MH.UI.WPF.Controls;

public class MediaPlayer : MediaElement, IPlatformSpecificUiMediaPlayer {
  private UIC.MediaPlayer _model;

  static MediaPlayer() {
    DefaultStyleKeyProperty.OverrideMetadata(
      typeof(MediaPlayer),
      new FrameworkPropertyMetadata(typeof(MediaPlayer)));
  }

  public MediaPlayer() {
    LoadedBehavior = MediaState.Manual;
    UnloadedBehavior = MediaState.Stop;
    ScrubbingEnabled = true;
    Stretch = Stretch.Uniform;
    StretchDirection = StretchDirection.Both;
    MediaOpened += OnMediaOpened;
    MediaEnded += OnMediaEnded;
  }

  public void SetModel(UIC.MediaPlayer model) {
    _model = model;
    if (_model == null) return;
    _model.UiMediaPlayer = this;
    SpeedRatio = _model.Speed;
    Volume = _model.Volume;
    IsMuted = _model.IsMuted;

    if (!string.IsNullOrEmpty(_model.Source))
      Source = new(_model.Source);
  }

  public void UnsetModel() {
    Pause();
    Source = null;
    if (_model == null) return;
    _model.UiMediaPlayer = null;
    _model = null;
  }

  private void OnMediaEnded(object sender, RoutedEventArgs e) {
    _model?.OnMediaEnded();
  }

  private void OnMediaOpened(object sender, RoutedEventArgs e) {
    if (HasVideo)
      _model?.OnMediaOpened(NaturalDuration.HasTimeSpan
        ? (int)NaturalDuration.TimeSpan.TotalMilliseconds
        : 0);
  }
}