﻿using PictureManager.Domain.Models;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PictureManager.ViewModels; 

public static class HideMouseTimer {
  private static bool _isHidden;
  private static DispatcherTimer _timer;
  private static readonly TimeSpan _interval = TimeSpan.FromMilliseconds(3000);
  private static DateTime _lastMove;

  public static void Init(Window window, PresentationPanelM panel) {
    _timer = new() { Interval = _interval };
    _timer.Tick += delegate {
      if (_isHidden || !window.IsActive || DateTime.Now - _lastMove < _interval)
        return;
        
      _isHidden = true;
      Mouse.OverrideCursor = Cursors.None;
    };

    panel.PropertyChanged += (_, e) => {
      if (!nameof(panel.IsRunning).Equals(e.PropertyName)) return;
      if (panel.IsRunning) {
        window.MouseMove += OnMouseMoveHideTimer;
        _timer.Start();
      }
      else {
        window.MouseMove -= OnMouseMoveHideTimer;
        _timer.Stop();
        OnMouseMoveHideTimer(null, null);
      }
    };
  }

  private static void OnMouseMoveHideTimer(object sender, MouseEventArgs args) {
    _lastMove = DateTime.Now;
    if (!_isHidden) return;
    _isHidden = false;
    Mouse.OverrideCursor = null;
  }
}