using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using System;
using System.Collections.Generic;

namespace PictureManager.Domain.Models {
  public sealed class MediaViewerM : ObservableObject {
    private readonly Core _core;
    private double _scale;
    private int _contentWidth;
    private int _contentHeight;
    private int _indexOfCurrent;
    private MediaItemM _current;
    private bool _isVisible;
    private bool _reScaleToFit;

    public MediaPlayerM MediaPlayerM { get; }
    public Func<string, string, object[]> GetVideoMetadata { get; set; }
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
    public MediaItemM Current {
      get => _current;
      set {
        _current = value;

        OnPropertyChanged();
        OnPropertyChanged(nameof(PositionSlashCount));
        SetMediaItemSource(value);

        if (value != null) {
          var rotated = value.Orientation is (int)MediaOrientation.Rotate90 or (int)MediaOrientation.Rotate270;

          ContentWidth = rotated ? value.Height : value.Width;
          ContentHeight = rotated ? value.Width : value.Height;
          ReScaleToFit = true;
        }
      }
    }

    public double ActualZoom => Scale * 100;
    public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }
    public bool ReScaleToFit { get => _reScaleToFit; set { _reScaleToFit = value; OnPropertyChanged(); } }
    public string PositionSlashCount => $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{MediaItems?.Count}";
    public List<MediaItemM> MediaItems { get; private set; }
    public PresentationPanelM PresentationPanel { get; }

    public RelayCommand<object> NextCommand { get; }
    public RelayCommand<object> PreviousCommand { get; }
    public RelayCommand<MouseWheelEventArgs> NavigateCommand { get; }

    public MediaViewerM(Core core) {
      _core = core;
      MediaPlayerM = new();
      PresentationPanel = new(this);
      NextCommand = new(Next, CanNext);
      PreviousCommand = new(Previous, CanPrevious);
      NavigateCommand = new(Navigate);

      MediaPlayerM.RepeatEnded += () => {
        if (PresentationPanel.IsPaused)
          PresentationPanel.Start(Current, false);
      };
    }

    public void Deactivate() {
      PresentationPanel.Stop();
      MediaItems.Clear();
      SetMediaItemSource(null);
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
      PresentationPanel.Next(Current);
    }

    public bool CanPrevious() =>
      _indexOfCurrent > 0;

    public void Previous() {
      if (PresentationPanel.IsRunning)
        PresentationPanel.Stop();

      Current = MediaItems[--_indexOfCurrent];
    }

    private void Navigate(MouseWheelEventArgs e) {
      if (e.IsCtrlOn) return;
      if (e.Delta < 0) {
        if (CanNext())
          Next();
      }
      else {
        if (CanPrevious())
          Previous();
      }
    }

    private void SetMediaItemSource(MediaItemM mediaItem) {
      if (mediaItem == null || mediaItem.MediaType != MediaType.Video) {
        _core.VideoClipsM.SetMediaItem(null);
        MediaPlayerM.IsPlaying = false;
        MediaPlayerM.Source = String.Empty;
        _core.ToolsTabsM.Deactivate(_core.VideoClipsM);
      }

      if (mediaItem == null) return;

      if (mediaItem.MediaType == MediaType.Video) {
        var data = GetVideoMetadata(mediaItem.Folder.FullPath, mediaItem.FileName);
        var fps = (double)data[3] > 0 ? (double)data[3] : 30.0;
        var smallChange = Math.Round(1000 / fps, 0);

        _core.VideoClipsM.SetMediaItem(mediaItem);
        MediaPlayerM.Source = mediaItem.FilePath;
        MediaPlayerM.TimelineSmallChange = smallChange;
        _core.ToolsTabsM.Activate(_core.VideoClipsM, "Clips");
      }
    }
  }
}
