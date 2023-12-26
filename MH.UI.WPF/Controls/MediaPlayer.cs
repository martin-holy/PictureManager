using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UIC = MH.UI.Controls;

namespace MH.UI.WPF.Controls;

public class MediaPlayer : MediaElement {
  private bool _modelSet;

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
  }

  public void SetModel(UIC.MediaPlayer model) {
    if (_modelSet) return;
    _modelSet = true;

    model.PlayAction = Play;
    model.PauseAction = Pause;
    model.SetPositionAction = ms => Position = new(0, 0, 0, 0, ms);
    model.GetPositionFunc = () => Position.TotalMilliseconds;
    model.SetSource = s => Source = string.IsNullOrEmpty(s) ? null : new(s);

    model.PropertyChanged += (_, e) => {
      switch (e.PropertyName) {
        case nameof(model.Speed): SpeedRatio = model.Speed; break;
        case nameof(model.Volume): Volume = model.Volume; break;
        case nameof(model.IsMuted): IsMuted = model.IsMuted; break;
      }
    };

    MediaOpened += delegate {
      if (HasVideo)
        model.OnMediaOpened(NaturalDuration.HasTimeSpan
          ? (int)NaturalDuration.TimeSpan.TotalMilliseconds
          : 0);
    };

    MediaEnded += delegate {
      model.OnMediaEnded();
    };
  }
}