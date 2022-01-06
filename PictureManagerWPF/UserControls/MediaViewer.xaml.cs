using System;
using PictureManager.Domain;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MH.UI.WPF.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.ViewModels;

namespace PictureManager.UserControls {
  public partial class MediaViewer : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged(this, new(name));

    private int _indexOfCurrent;
    private MediaItemM _current;

    public MediaItemM Current {
      get => _current;
      set {
        if (_current != null)
          App.Core.ThumbnailsGridsM.Current?.SetSelected(_current, false);
        _current = value;
        if (_current != null)
          App.Core.ThumbnailsGridsM.Current?.SetSelected(_current, true);
        if (App.Core.MediaItemsM.Current != value)
          App.Core.MediaItemsM.Current = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(PositionSlashCount));
      }
    }

    public string PositionSlashCount => $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{MediaItems?.Count}";
    public List<MediaItemM> MediaItems { get; private set; }
    public PresentationPanelVM PresentationPanel { get; }

    public RelayCommand<object> NextCommand { get; }
    public RelayCommand<object> PreviousCommand { get; }

    public MediaViewer() {
      PresentationPanel = new(this);
      NextCommand = new(Next, CanNext);
      PreviousCommand = new(Previous, CanPrevious);

      InitializeComponent();
      AttachEvents();
    }

    private void AttachEvents() {
      MouseWheel += (_, e) => {
        if ((Keyboard.Modifiers & ModifierKeys.Control) > 0) return;
        if (e.Delta < 0) {
          if (CanNext())
            Next();
        }
        else {
          if (CanPrevious())
            Previous();
        }
      };

      FullVideo.RepeatEnded += () => {
        if (!PresentationPanel.IsPaused) return;
        PresentationPanel.Start(false);
      };

      PreviewMouseDown += SegmentsRects.OnPreviewMouseDown;
      PreviewMouseMove += SegmentsRects.OnPreviewMouseMove;
      PreviewMouseUp += SegmentsRects.OnPreviewMouseUp;

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
      App.WMain.ToolsTabs.Activate(App.WMain.ToolsTabs.TabClips, false);
    }

    public void SetMediaItems(List<MediaItemM> mediaItems) {
      if (mediaItems == null || mediaItems.Count == 0) {
        MediaItems.Clear();
        Current = null;
      }
      else {
        foreach (var mi in mediaItems)
          mi.SetInfoBox();

        MediaItems = mediaItems;
        _indexOfCurrent = 0;
        Current = mediaItems[0];
      }
    }

    public void SetMediaItemSource(MediaItemM mediaItem) {
      var index = MediaItems.IndexOf(mediaItem);
      if (index < 0) return;
      _indexOfCurrent = index;
      Current = mediaItem;
      App.Core.SegmentsM.SegmentsRectsM.MediaItem = mediaItem;

      switch (mediaItem.MediaType) {
        case MediaType.Image: {
          FullImage.SetSource(mediaItem.FilePath, Imaging.MediaOrientation2Rotation((MediaOrientation)mediaItem.Orientation));
          App.Ui.VideoClipsTreeVM.SetMediaItem(null);
          FullVideo.SetNullSource();
          App.WMain.ToolsTabs.Activate(App.WMain.ToolsTabs.TabClips, false);
          break;
        }
        case MediaType.Video: {
          var data = ShellStuff.FileInformation.GetVideoMetadata(mediaItem.Folder.FullPath, mediaItem.FileName);
          var fps = (double)data[3] > 0 ? (double)data[3] : 30.0;
          var smallChange = Math.Round(1000 / fps, 0);

          App.Ui.VideoClipsTreeVM.SetMediaItem(mediaItem);
          FullVideo.SetSource(mediaItem.FilePath, mediaItem.RotationAngle, smallChange);
          App.WMain.ToolsTabs.Activate(App.WMain.ToolsTabs.TabClips, true);
          break;
        }
      }
    }

    public bool CanNext() =>
      MediaItems.Count > 0 && _indexOfCurrent < MediaItems.Count - 1;

    public void Next() {
      Current = MediaItems[++_indexOfCurrent];
      SetMediaItemSource(Current);

      if (PresentationPanel.IsRunning && (Current.MediaType == MediaType.Video ||
        (Current.IsPanoramic && PresentationPanel.PlayPanoramicImages))) {

        PresentationPanel.Pause();

        if (Current.MediaType == MediaType.Image && Current.IsPanoramic)
          PresentationPanel.Start(true);
      }

      App.Ui.MarkUsedKeywordsAndPeople();
    }

    public bool CanPrevious() =>
      _indexOfCurrent > 0;

    public void Previous() {
      if (PresentationPanel.IsRunning)
        PresentationPanel.Stop();

      Current = MediaItems[--_indexOfCurrent];
      SetMediaItemSource(Current);
      App.Ui.MarkUsedKeywordsAndPeople();
    }
  }
}
