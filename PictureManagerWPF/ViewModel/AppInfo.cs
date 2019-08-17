using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using PictureManager.Database;

namespace PictureManager.ViewModel {
  public class AppInfoRating {
    public IconName IconName { get; set; }
  }

  public class AppInfo : INotifyPropertyChanged {
    private int _selected;
    private int _modifed;
    private int _progressBarValue;
    private bool _progressBarIsIndeterminate;
    private string _positionSlashCount;
    private BaseMediaItem _currentMediaItem;
    private AppMode _appMode;
    private bool _isThumbInfoVisible = true;
    private int _mediaItemsCount;

    public int Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public int Modifed { get => _modifed; set { _modifed = value; OnPropertyChanged(); } }
    public int ProgressBarValue { get => _progressBarValue; set { _progressBarValue = value; OnPropertyChanged(); } }
    public bool ProgressBarIsIndeterminate { get => _progressBarIsIndeterminate; set { _progressBarIsIndeterminate = value; OnPropertyChanged(); } }
    public string PositionSlashCount { get => _positionSlashCount; set { _positionSlashCount = value; OnPropertyChanged(); } }
    public bool IsThumbInfoVisible { get => _isThumbInfoVisible; set { _isThumbInfoVisible = value; OnPropertyChanged(); } }
    public int MediaItemsCount { get => _mediaItemsCount; set { _mediaItemsCount = value; OnPropertyChanged(); } }
    public string Comment { get; set; } = string.Empty;
    public ObservableCollection<AppInfoRating> Rating { get; } = new ObservableCollection<AppInfoRating>();
    public AppMode AppMode { get => _appMode; set { _appMode = value; OnPropertyChanged(); } }
    public string Dimension { get; set; } = string.Empty;
    public string FullGeoName { get; set; } = string.Empty;
    public event PropertyChangedEventHandler PropertyChanged;

    public string FilePath {
      get {
        if (CurrentMediaItem == null) return string.Empty;
        if (AppMode == AppMode.Viewer && CurrentMediaItem.Folder.FolderKeyword != null) 
          return $"{CurrentMediaItem.Folder.FolderKeyword.Title}\\{CurrentMediaItem.FileName}";
        return CurrentMediaItem.FilePath;
      }
    }

    public BaseMediaItem CurrentMediaItem {
      get => _currentMediaItem;
      set {
        _currentMediaItem = value;
        OnPropertyChanged();
        OnPropertyChanged($"FilePath");

        Rating.Clear();
        for (var i = 0; i < _currentMediaItem?.Rating; i++) 
          Rating.Add(new AppInfoRating {IconName = IconName.Star });

        Comment = _currentMediaItem == null ? string.Empty : _currentMediaItem.CommentEscaped;
        OnPropertyChanged($"Comment");

        Dimension = _currentMediaItem == null ? string.Empty : $"{_currentMediaItem.Width}x{_currentMediaItem.Height}";
        OnPropertyChanged($"Dimension");

        var aCore = (AppCore) Application.Current.Properties[nameof(AppProperty.AppCore)];
        FullGeoName = aCore.GeoNames.GetGeoNameHierarchy(_currentMediaItem?.GeoName);
        OnPropertyChanged($"FullGeoName");
      }
    }

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
