using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using PictureManager.Database;

namespace PictureManager.ViewModel {
  public class AppInfoRating {
    public IconName IconName { get; set; }
  }

  public class AppInfo : INotifyPropertyChanged {
    private int _progressBarValue;
    private bool _progressBarIsIndeterminate;
    private string _positionSlashCount;
    private MediaItem _currentMediaItem;
    private AppMode _appMode;
    private bool _isThumbInfoVisible = true;
    private int _mediaItemsCount;
    private string _dimension = string.Empty;
    private string _fullGeoName = string.Empty;

    public int ProgressBarValue { get => _progressBarValue; set { _progressBarValue = value; OnPropertyChanged(); } }
    public bool ProgressBarIsIndeterminate { get => _progressBarIsIndeterminate; set { _progressBarIsIndeterminate = value; OnPropertyChanged(); } }
    public string PositionSlashCount { get => _positionSlashCount; set { _positionSlashCount = value; OnPropertyChanged(); } }
    public bool IsThumbInfoVisible { get => _isThumbInfoVisible; set { _isThumbInfoVisible = value; OnPropertyChanged(); } }
    public int MediaItemsCount { get => _mediaItemsCount; set { _mediaItemsCount = value; OnPropertyChanged(); } }
    public string Dimension { get => _dimension; set { _dimension = value; OnPropertyChanged(); } }
    public string FullGeoName { get => _fullGeoName; set { _fullGeoName = value; OnPropertyChanged(); } }
    public ObservableCollection<AppInfoRating> Rating { get; } = new ObservableCollection<AppInfoRating>();

    public bool IsGeoNameVisible => CurrentMediaItem?.GeoName != null;
    public bool IsCommentVisible => AppMode == AppMode.Viewer && !string.IsNullOrEmpty(CurrentMediaItem?.Comment);
    public bool IsInfoBoxPeopleVisible => AppMode == AppMode.Viewer && CurrentMediaItem?.InfoBoxPeople != null;
    public bool IsInfoBoxKeywordsVisible => AppMode == AppMode.Viewer && CurrentMediaItem?.InfoBoxKeywords != null;
    public bool IsImageActualZoomVisible => AppMode == AppMode.Viewer && CurrentMediaItem?.MediaType == MediaType.Image;

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public AppMode AppMode {
      get => _appMode;
      set {
        _appMode = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(IsCommentVisible));
        OnPropertyChanged(nameof(IsInfoBoxPeopleVisible));
        OnPropertyChanged(nameof(IsInfoBoxKeywordsVisible));
        OnPropertyChanged(nameof(IsImageActualZoomVisible));
      }
    }

    public string FilePath {
      get {
        if (CurrentMediaItem == null) return string.Empty;
        if (AppMode == AppMode.Viewer && CurrentMediaItem.Folder.FolderKeyword != null)
          return $"{CurrentMediaItem.Folder.FolderKeyword.Title}{Path.DirectorySeparatorChar}{CurrentMediaItem.FileName}";
        return CurrentMediaItem.FilePath;
      }
    }

    public MediaItem CurrentMediaItem {
      get => _currentMediaItem;
      set {
        _currentMediaItem = value;
        OnPropertyChanged();

        Rating.Clear();
        for (var i = 0; i < _currentMediaItem?.Rating; i++) 
          Rating.Add(new AppInfoRating {IconName = IconName.Star});

        Dimension = _currentMediaItem == null ? string.Empty : $"{_currentMediaItem.Width}x{_currentMediaItem.Height}";
        FullGeoName = _currentMediaItem?.GeoName?.GetFullPath("\n");
        PositionSlashCount = _currentMediaItem == null
          ? App.Core.MediaItems.Items.Count.ToString()
          : $"{_currentMediaItem.Index + 1}/{App.Core.MediaItems.Items.Count}";

        OnPropertyChanged(nameof(IsGeoNameVisible));
        OnPropertyChanged(nameof(IsCommentVisible));
        OnPropertyChanged(nameof(IsInfoBoxPeopleVisible));
        OnPropertyChanged(nameof(IsInfoBoxKeywordsVisible));
        OnPropertyChanged(nameof(IsImageActualZoomVisible));
        OnPropertyChanged(nameof(FilePath));
      }
    }
  }
}
