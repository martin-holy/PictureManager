using PictureManager.Commands;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class MediaViewer : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // local props
    private int _indexOfCurrent;

    private MediaItem _current;

    // public props
    public MediaItem Current {
      get => _current;
      set {
        _current = value;
        if (App.Core.MediaItems.Current != value)
          App.Core.MediaItems.Current = value;
        OnPropertyChanged();
        UpdatePositionSlashCount();
      }
    }
    public List<MediaItem> MediaItems { get; private set; }

    // commands
    public static RoutedUICommand NextCommand { get; } = CommandsController.CreateCommand("Next", "Next", new KeyGesture(Key.Right));
    public static RoutedUICommand PreviousCommand { get; } = CommandsController.CreateCommand("Previous", "Previous", new KeyGesture(Key.Left));
    public static RoutedUICommand PresentationCommand { get; } = CommandsController.CreateCommand("Presentation", "Presentation", new KeyGesture(Key.P, ModifierKeys.Control));

    public MediaViewer() {
      InitializeComponent();
      AttachEvents();

      FullVideo.ApplyTemplate();
      FullVideo.MediaItemClips.Add(App.Ui.MediaItemClipsCategory);
    }

    private void AttachEvents() {
      MouseLeftButtonDown += (o, e) => {
        if (e.ClickCount == 2)
          WindowCommands.SwitchToBrowser();
      };

      MouseWheel += (o, e) => {
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

      PresentationPanel.Elapsed = delegate {
        App.Core.RunOnUiThread(() => {
          if (PresentationPanel.IsPaused) return;
          if (CanNext())
            Next();
          else
            PresentationPanel.Stop();
        });
      };

      FullVideo.RepeatEnded += delegate {
        if (!PresentationPanel.IsPaused) return;
        PresentationPanel.Start(false);
      };

      Loaded += (o, e) => SetUpCommands(App.WMain.CommandBindings);
    }

    private void SetUpCommands(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, NextCommand, Next, CanNext);
      CommandsController.AddCommandBinding(cbc, PreviousCommand, Previous, CanPrevious);
      CommandsController.AddCommandBinding(cbc, PresentationCommand, Presentation, CanPresentation);
    }

    public void Deactivate() {
      PresentationPanel.Stop();
      FullImage.Stop();
      FullImage.SetSource(null);
      FullVideo.IsPlaying = false;
      FullVideo.SetSource(null);
      MediaItems.Clear();
    }

    public void SetMediaItems(List<MediaItem> mediaItems) {
      if (mediaItems == null || mediaItems.Count == 0) {
        MediaItems.Clear();
        Current = null;
      }
      else {
        foreach (var mi in mediaItems)
          mi.SetInfoBox();

        MediaItems = mediaItems;
        Current = mediaItems[0];
        _indexOfCurrent = 0;
      }

      UpdatePositionSlashCount();
    }

    public void SetMediaItemSource(MediaItem mediaItem, bool decoded = false) {
      var index = MediaItems.IndexOf(mediaItem);
      if (index < 0) return;
      _indexOfCurrent = index;
      Current = mediaItem;

      switch (mediaItem.MediaType) {
        case MediaType.Image: {
          FullImage.SetSource(mediaItem, decoded);
          App.Ui.MediaItemClipsCategory.SetMediaItem(null);
          FullVideo.SetSource(null);
          break;
        }
        case MediaType.Video: {
          App.Ui.MediaItemClipsCategory.SetMediaItem(mediaItem);
          FullVideo.SetSource(mediaItem);
          break;
        }
      }
    }

    private void UpdatePositionSlashCount() =>
      App.Core.MediaItems.PositionSlashCount = $"{(Current == null ? string.Empty : $"{_indexOfCurrent + 1}/")}{MediaItems.Count}";

    #region Commands

    public bool CanNext() => MediaItems.Count > 0 && _indexOfCurrent < MediaItems.Count - 1;

    public void Next() {
      Current = MediaItems[++_indexOfCurrent];

      var decoded = PresentationPanel.IsRunning && Current.MediaType == MediaType.Image && Current.IsPanoramic;
      SetMediaItemSource(Current, decoded);

      if (PresentationPanel.IsRunning && (Current.MediaType == MediaType.Video ||
        (Current.IsPanoramic && PresentationPanel.PlayPanoramicImages))) {

        PresentationPanel.Pause();

        if (Current.MediaType == MediaType.Image && Current.IsPanoramic)
          PresentationPanel.Start(true);
      }

      App.Core.MarkUsedKeywordsAndPeople();
    }

    public bool CanPrevious() => _indexOfCurrent > 0;

    public void Previous() {
      if (PresentationPanel.IsRunning)
        PresentationPanel.Stop();

      Current = MediaItems[--_indexOfCurrent];
      SetMediaItemSource(Current);
      App.Core.MarkUsedKeywordsAndPeople();
    }

    private bool CanPresentation() => Current != null;

    private void Presentation() {
      if (FullImage.IsAnimationOn) {
        FullImage.Stop();
        PresentationPanel.Stop();
        return;
      }

      if (PresentationPanel.IsRunning || PresentationPanel.IsPaused)
        PresentationPanel.Stop();
      else
        PresentationPanel.Start(true);
    }

    #endregion
  }
}
