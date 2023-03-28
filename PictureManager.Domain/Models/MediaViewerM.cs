using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

namespace PictureManager.Domain.Models {
  public sealed class MediaViewerM : ObservableObject {
    private int _contentWidth;
    private int _contentHeight;
    private int _indexOfCurrent;
    private MediaItemM _current;
    private double _actualZoom;
    private bool _isVisible;
    private bool _reScaleToFit;

    public MediaPlayerM MediaPlayerM { get; }
    public int ContentWidth { get => _contentWidth; set { _contentWidth = value; OnPropertyChanged(); } }
    public int ContentHeight { get => _contentHeight; set { _contentHeight = value; OnPropertyChanged(); } }
    public MediaItemM Current {
      get => _current;
      set {
        _current = value;

        OnPropertyChanged();
        OnPropertyChanged(nameof(PositionSlashCount));

        if (value != null) {
          var rotated = value.Orientation is (int)MediaOrientation.Rotate90 or (int)MediaOrientation.Rotate270;

          ContentWidth = rotated ? value.Height : value.Width;
          ContentHeight = rotated ? value.Width : value.Height;
          ReScaleToFit = true;
        }
      }
    }

    public double ActualZoom { get => _actualZoom; set { _actualZoom = value; OnPropertyChanged(); } }
    public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }
    public bool ReScaleToFit { get => _reScaleToFit; set { _reScaleToFit = value; OnPropertyChanged(); } }
    public string PositionSlashCount => $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{MediaItems?.Count}";
    public List<MediaItemM> MediaItems { get; private set; }
    public PresentationPanelM PresentationPanel { get; }

    public RelayCommand<object> NextCommand { get; }
    public RelayCommand<object> PreviousCommand { get; }
    public RelayCommand<double> UpdateActualZoomCommand { get; }

    public MediaViewerM() {
      MediaPlayerM = new();
      PresentationPanel = new(this);
      NextCommand = new(Next, CanNext);
      PreviousCommand = new(Previous, CanPrevious);
      UpdateActualZoomCommand = new(scale => ActualZoom = scale * 100);

      MediaPlayerM.RepeatEnded += () => {
        if (PresentationPanel.IsPaused)
          PresentationPanel.Start(false);
      };
    }

    public void Deactivate() {
      PresentationPanel.Stop();
      MediaItems.Clear();
      Current = null;
    }

    public void SetCurrent(MediaItemM current) {
      if (IsVisible && Current != current)
        Current = current;
    }

    public void SetMediaItems(List<MediaItemM> mediaItems, MediaItemM current) {
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

      if (PresentationPanel.IsRunning && (Current.MediaType == MediaType.Video ||
        (Current.IsPanoramic && PresentationPanel.PlayPanoramicImages))) {

        PresentationPanel.Pause();

        if (Current.MediaType == MediaType.Image && Current.IsPanoramic)
          PresentationPanel.Start(true);
      }
    }

    public bool CanPrevious() =>
      _indexOfCurrent > 0;

    public void Previous() {
      if (PresentationPanel.IsRunning)
        PresentationPanel.Stop();

      Current = MediaItems[--_indexOfCurrent];
    }
  }
}
