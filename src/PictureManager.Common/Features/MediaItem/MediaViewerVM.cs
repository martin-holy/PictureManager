using MH.UI.Controls;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using System.Collections.Generic;

namespace PictureManager.Common.Features.MediaItem;

public sealed class MediaViewerVM : ObservableObject {
  public enum UserInputModes {
    Browse = 1,
    Transform = 2
  }

  private readonly MediaItemVM _mediaItemVM;
  private int _indexOfCurrent;
  private MediaItemFullVM? _currentFull;
  private UserInputModes _userInputMode;
  private bool _expandToFill;

  public MediaItemFullVM? CurrentFull { get => _currentFull; set { _currentFull = value; OnPropertyChanged(); } }
  public int IndexOfCurrent { get => _indexOfCurrent; }
  public UserInputModes UserInputMode { get => _userInputMode; set { _userInputMode = value; OnPropertyChanged(); } }
  public bool ExpandToFill { get => _expandToFill; set { _expandToFill = value; OnPropertyChanged(); } }
  public string PositionSlashCount => $"{_indexOfCurrent + 1}/{MediaItems.Count}";
  public List<MediaItemM> MediaItems { get; private set; } = [];
  public SlideshowVM Slideshow { get; }

  public RelayCommand NextCommand { get; }
  public RelayCommand PreviousCommand { get; }
  public RelayCommand<MouseWheelEventArgs> NavigateCommand { get; }

  public MediaViewerVM(MediaItemVM mediaItemVM) {
    _mediaItemVM = mediaItemVM;
    _expandToFill = Core.Settings.MediaViewer.ExpandToFill;

    Slideshow = new(this);
    NextCommand = new(_next, _canNext);
    PreviousCommand = new(_previous, _canPrevious);
    NavigateCommand = new(_navigate);
  }

  public void Deactivate() {
    Slideshow.Stop();
    MediaItems.Clear();
    OnPropertyChanged(nameof(MediaItems));
  }

  public void SetMediaItems(List<MediaItemM>? mediaItems, MediaItemM current) {
    if (mediaItems == null || mediaItems.Count == 0) {
      MediaItems.Clear();
      _mediaItemVM.Current = null;
    }
    else {
      foreach (var mi in mediaItems)
        mi.SetInfoBox();

      _indexOfCurrent = mediaItems.IndexOf(current);
      MediaItems = mediaItems;
      _mediaItemVM.Current = current;
    }
    OnPropertyChanged(nameof(MediaItems));
    OnPropertyChanged(nameof(PositionSlashCount));
  }

  public bool Next() {
    if (!_canNext()) return false;
    _next();
    return true;
  }

  public void GoTo(int index) {
    if (index < 0 || index > MediaItems.Count) return;
    Slideshow.Stop();
    _indexOfCurrent = index;
    _mediaItemVM.Current = MediaItems[_indexOfCurrent];
    OnPropertyChanged(nameof(PositionSlashCount));
  }

  private bool _canNext() =>
    MediaItems.Count > 0 && _indexOfCurrent < MediaItems.Count - 1;

  private void _next() {
    _mediaItemVM.Current = MediaItems[++_indexOfCurrent];
    OnPropertyChanged(nameof(PositionSlashCount));
  }

  private bool _canPrevious() =>
    _indexOfCurrent > 0;

  private void _previous() {
    Slideshow.Stop();
    _mediaItemVM.Current = MediaItems[--_indexOfCurrent];
    OnPropertyChanged(nameof(PositionSlashCount));
  }

  private void _navigate(MouseWheelEventArgs? e) {
    if (e == null || Keyboard.IsCtrlOn()) return;
    if (e.Delta < 0) {
      if (_canNext()) _next();
    }
    else {
      if (_canPrevious()) _previous();
    }
  }

  public void Remove(MediaItemM oldMi, MediaItemM? newMi) {
    if (!MediaItems.Remove(oldMi) || newMi == null) return;
    _indexOfCurrent = MediaItems.IndexOf(newMi);
    _mediaItemVM.Current = newMi;
    OnPropertyChanged(nameof(MediaItems));
  }
}