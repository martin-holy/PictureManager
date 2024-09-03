﻿using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using System.Collections.Generic;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaViewerVM : ObservableObject {
  private int _indexOfCurrent;

  private int _contentWidth;
  private int _contentHeight;
  private MediaItemM? _current;
  private bool _isVisible;

  public int ContentWidth { get => _contentWidth; set { _contentWidth = value; OnPropertyChanged(); } }
  public int ContentHeight { get => _contentHeight; set { _contentHeight = value; OnPropertyChanged(); } }
  public MediaItemM? Current { get => _current; set => _onCurrentChanged(value); }
  public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }
  public string PositionSlashCount => $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{MediaItems.Count}";
  public List<MediaItemM> MediaItems { get; private set; } = [];
  public SlideshowVM Slideshow { get; }
  public ZoomAndPan ZoomAndPan { get; } = new();

  public RelayCommand NextCommand { get; }
  public RelayCommand PreviousCommand { get; }
  public RelayCommand<MouseWheelEventArgs> NavigateCommand { get; }

  public MediaViewerVM() {
    ZoomAndPan.ExpandToFill = Core.Settings.MediaViewer.ExpandToFill;
    ZoomAndPan.ShrinkToFill = Core.Settings.MediaViewer.ShrinkToFill;

    Slideshow = new(this, ZoomAndPan);
    NextCommand = new(_next, _canNext);
    PreviousCommand = new(_previous, _canPrevious);
    NavigateCommand = new(_navigate);
  }

  private void _onCurrentChanged(MediaItemM? current) {
    if (!Core.S.MediaItem.Exists(current)) return;
    _current = current;
    OnPropertyChanged(nameof(Current));
    OnPropertyChanged(nameof(PositionSlashCount));

    if (current != null) {
      var rotated = current.Orientation is Orientation.Rotate90 or Orientation.Rotate270;

      ContentWidth = rotated ? current.Height : current.Width;
      ContentHeight = rotated ? current.Width : current.Height;
      ZoomAndPan.ScaleToFitContent(ContentWidth, ContentHeight);
      ZoomAndPan.StopAnimation();
      Slideshow.OnCurrentChanged(current);
    }
  }

  public void Deactivate() {
    Slideshow.Stop();
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

  public bool Next() {
    if (!_canNext()) return false;
    _next();
    return true;
  }

  private bool _canNext() =>
    MediaItems.Count > 0 && _indexOfCurrent < MediaItems.Count - 1;

  private void _next() {
    Current = MediaItems[++_indexOfCurrent];
  }

  private bool _canPrevious() =>
    _indexOfCurrent > 0;

  private void _previous() {
    Slideshow.Stop();
    Current = MediaItems[--_indexOfCurrent];
  }

  private void _navigate(MouseWheelEventArgs? e) {
    if (e == null || e.IsCtrlOn) return;
    if (e.Delta < 0) {
      if (_canNext()) _next();
    }
    else {
      if (_canPrevious()) _previous();
    }
  }

  public void Remove(MediaItemM oldMi, MediaItemM? newMi) {
    MediaItems.Remove(oldMi);
    if (newMi == null) return;
    _indexOfCurrent = MediaItems.IndexOf(newMi);
    Current = newMi;
  }
}