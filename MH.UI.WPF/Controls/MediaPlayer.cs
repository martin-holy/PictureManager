using MH.UI.Interfaces;
using System.Windows.Controls;
using System.Windows.Media;
using UIC = MH.UI.Controls;

namespace MH.UI.WPF.Controls;

public class MediaPlayer : MediaElement, IPlatformSpecificUiMediaPlayer {
  public UIC.MediaPlayer ViewModel { get; set; }

  public MediaPlayer() {
    LoadedBehavior = MediaState.Manual;
    UnloadedBehavior = MediaState.Close;
    ScrubbingEnabled = true;
    Stretch = Stretch.Uniform;
    StretchDirection = StretchDirection.Both;
    MediaEnded += delegate { ViewModel?.OnMediaEnded(); };
    MediaOpened += delegate {
      if (!HasVideo) return;
      ViewModel?.OnMediaOpened(NaturalDuration.HasTimeSpan
        ? (int)NaturalDuration.TimeSpan.TotalMilliseconds
        : 0);
    };
  }
}