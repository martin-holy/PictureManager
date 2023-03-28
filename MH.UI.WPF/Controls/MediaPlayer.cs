using System;
using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Controls {
  public class MediaPlayer : MediaElement {
    public static readonly DependencyProperty SpeedProperty =
      DependencyProperty.Register(nameof(Speed), typeof(double), typeof(MediaPlayer),
        new((o, e) => (o as MediaPlayer)?.SpeedChanged()));

    public double Speed {
      get => (double)GetValue(SpeedProperty);
      set => SetValue(SpeedProperty, value);
    }

    static MediaPlayer() {
      DefaultStyleKeyProperty.OverrideMetadata(
        typeof(MediaPlayer),
        new FrameworkPropertyMetadata(typeof(MediaPlayer)));
    }

    public void SpeedChanged() =>
      SpeedRatio = Speed;
  }
}
