using MH.UI.Controls;
using System;

namespace MH.UI.Interfaces;

public interface IPlatformSpecificUiMediaPlayer {
  public double SpeedRatio { get; set; }
  public double Volume { get; set; }
  public bool IsMuted { get; set; }
  public TimeSpan Position { get; set; }
  public Uri Source { get; set; }
  public void Play();
  public void Pause();
  public void SetModel(MediaPlayer model);
  public void UnsetModel();
}