﻿using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using System;
using System.Collections.Generic;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaViewerVM : ObservableObject {
  private double _scale;
  private int _contentWidth;
  private int _contentHeight;
  private int _indexOfCurrent;
  private MediaItemM? _current;
  private bool _isVisible;
  private bool _reScaleToFit;

  public double Scale {
    get => _scale;
    set {
      _scale = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof(ActualZoom));
    }
  }

  public int ContentWidth { get => _contentWidth; set { _contentWidth = value; OnPropertyChanged(); } }
  public int ContentHeight { get => _contentHeight; set { _contentHeight = value; OnPropertyChanged(); } }
  
  public MediaItemM? Current {
    get => _current;
    set {
      if (!Core.S.MediaItem.Exists(value)) return;
      _current = value;
      OnPropertyChanged();
      OnPropertyChanged(nameof(PositionSlashCount));

      if (value != null) {
        var rotated = value.Orientation is Orientation.Rotate90 or Orientation.Rotate270;

        ContentWidth = rotated ? value.Height : value.Width;
        ContentHeight = rotated ? value.Width : value.Height;
        ReScaleToFit = true;
      }
    }
  }

  public double ActualZoom => Scale * 100;
  public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }
  public bool ReScaleToFit { get => _reScaleToFit; set { _reScaleToFit = value; OnPropertyChanged(); } }
  public string PositionSlashCount => $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{MediaItems.Count}";
  public List<MediaItemM> MediaItems { get; private set; } = [];
  public PresentationPanelVM PresentationPanel { get; }

  public RelayCommand NextCommand { get; }
  public RelayCommand PreviousCommand { get; }
  public RelayCommand<MouseWheelEventArgs> NavigateCommand { get; }

  public MediaViewerVM() {
    PresentationPanel = new(this);
    NextCommand = new(Next, CanNext);
    PreviousCommand = new(Previous, CanPrevious);
    NavigateCommand = new(Navigate);
  }

  public void OnPlayerRepeatEnded(object? sender, EventArgs e) {
    if (PresentationPanel.IsPaused && Current != null)
      PresentationPanel.Start(Current, false);
  }

  public void Deactivate() {
    PresentationPanel.Stop();
    MediaItems.Clear();
  }

  public void SetMediaItems(List<MediaItemM>? mediaItems, MediaItemM current) {
    if (mediaItems == null || mediaItems.Count == 0) {
      MediaItems.Clear();
      Current = null;
    }
    else {
      foreach (var mi in mediaItems)
        mi.SetInfoBox();

      _indexOfCurrent = mediaItems.IndexOf(current);
      MediaItems = mediaItems;
      Current = current;
    }
  }

  public bool CanNext() =>
    MediaItems.Count > 0 && _indexOfCurrent < MediaItems.Count - 1;

  public void Next() {
    Current = MediaItems[++_indexOfCurrent];
    PresentationPanel.Next(Current);
  }

  public bool CanPrevious() =>
    _indexOfCurrent > 0;

  public void Previous() {
    if (PresentationPanel.IsRunning)
      PresentationPanel.Stop();

    Current = MediaItems[--_indexOfCurrent];
  }

  private void Navigate(MouseWheelEventArgs? e) {
    if (e == null || e.IsCtrlOn) return;
    if (e.Delta < 0) {
      if (CanNext())
        Next();
    }
    else {
      if (CanPrevious())
        Previous();
    }
  }

  public void Remove(MediaItemM oldMi, MediaItemM? newMi) {
    MediaItems.Remove(oldMi);
    if (newMi == null) return;
    _indexOfCurrent = MediaItems.IndexOf(newMi);
    Current = newMi;
  }
}