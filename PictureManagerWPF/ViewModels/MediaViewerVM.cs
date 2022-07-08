using System;
using System.Collections.Generic;
using System.Windows.Input;
using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;

namespace PictureManager.ViewModels {
  public sealed class MediaViewerVM : ObservableObject {
    private int _indexOfCurrent;
    private MediaItemM _current;
    private bool _isVisible;

    public MediaItemM Current {
      get => _current;
      set {
        _current = value;

        OnPropertyChanged();
        OnPropertyChanged(nameof(PositionSlashCount));
      }
    }

    public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }
    public string PositionSlashCount => $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{MediaItems?.Count}";
    public List<MediaItemM> MediaItems { get; private set; }
    public PresentationPanelVM PresentationPanel { get; }
    public ZoomAndPanImage FullImage { get; }
    public VideoPlayer FullVideo { get; }

    public RelayCommand<object> NextCommand { get; }
    public RelayCommand<object> PreviousCommand { get; }
    public RelayCommand<MouseWheelEventArgs> NavigateCommand { get; }

    public MediaViewerVM() {
      PresentationPanel = new(this);
      NextCommand = new(Next, CanNext);
      PreviousCommand = new(Previous, CanPrevious);
      NavigateCommand = new(Navigate);

      FullImage = new();
      FullVideo = new();

      FullVideo.ApplyTemplate();

      AttachEvents();
    }

    private void AttachEvents() {
      FullVideo.RepeatEnded += () => {
        if (!PresentationPanel.IsPaused) return;
        PresentationPanel.Start(false);
      };

      FullImage.ScaleChangedEventHandler += (_, _) =>
        App.Core.SegmentsM.SegmentsRectsM.Scale = FullImage.ScaleX;
    }

    public void Deactivate() {
      PresentationPanel.Stop();
      FullImage.Stop();
      FullImage.SetSource(null, 0);
      FullVideo.IsPlaying = false;
      FullVideo.SetNullSource();
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

        MediaItems = mediaItems;
        SetMediaItemSource(current);
      }
    }

    public void SetMediaItemSource(MediaItemM mediaItem) {
      _indexOfCurrent = MediaItems.IndexOf(mediaItem);
      Current = mediaItem;

      switch (mediaItem.MediaType) {
        case MediaType.Image: {
          FullImage.SetSource(mediaItem.FilePath, Imaging.MediaOrientation2Rotation((MediaOrientation)mediaItem.Orientation));
          App.Core.VideoClipsM.SetMediaItem(null);
          FullVideo.SetNullSource();
          App.Ui.ToolsTabsVM.Deactivate(App.Ui.VideoClipsVM.ToolsTabsItem);
          break;
        }
        case MediaType.Video: {
          var data = ShellStuff.FileInformation.GetVideoMetadata(mediaItem.Folder.FullPath, mediaItem.FileName);
          var fps = (double)data[3] > 0 ? (double)data[3] : 30.0;
          var smallChange = Math.Round(1000 / fps, 0);

          App.Core.VideoClipsM.SetMediaItem(mediaItem);
          FullVideo.SetSource(mediaItem.FilePath, mediaItem.RotationAngle, smallChange);
          App.Ui.ToolsTabsVM.Activate(App.Ui.VideoClipsVM.ToolsTabsItem);
          break;
        }
      }
    }

    public bool CanNext() =>
      MediaItems.Count > 0 && _indexOfCurrent < MediaItems.Count - 1;

    public void Next() {
      SetMediaItemSource(MediaItems[++_indexOfCurrent]);

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

      SetMediaItemSource(MediaItems[--_indexOfCurrent]);
    }

    private void Navigate(MouseWheelEventArgs e) {
      if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) return;
      if (e.Delta < 0) {
        if (CanNext())
          Next();
      }
      else {
        if (CanPrevious())
          Previous();
      }
    }
  }
}
