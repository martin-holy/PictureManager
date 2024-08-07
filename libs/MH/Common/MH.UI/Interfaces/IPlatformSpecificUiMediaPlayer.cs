﻿using System;
using MH.UI.Controls;

namespace MH.UI.Interfaces;

public interface IPlatformSpecificUiMediaPlayer {
  public MediaPlayer? ViewModel { get; set; }
  public double SpeedRatio { get; set; }
  public double Volume { get; set; }
  public bool IsMuted { get; set; }
  public TimeSpan Position { get; set; }
  public Uri? Source { get; set; }
  public void Play();
  public void Pause();
}