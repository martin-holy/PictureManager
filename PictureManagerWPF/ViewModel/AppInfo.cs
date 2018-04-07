using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PictureManager.ViewModel {
  public class AppInfoRating {
    public IconName IconName { get; set; }
  }

  public class AppInfo : INotifyPropertyChanged {
    private int _selected;
    private int _modifed;
    private int _progressBarValue;
    private string _positionSlashCount;
    private BaseMediaItem _currentMediaItem;
    private AppMode _appMode;
    private bool _isThumbInfoVisible = true;

    public int Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public int Modifed { get => _modifed; set { _modifed = value; OnPropertyChanged(); } }
    public int ProgressBarValue { get => _progressBarValue; set { _progressBarValue = value; OnPropertyChanged(); } }
    public string PositionSlashCount { get => _positionSlashCount; set { _positionSlashCount = value; OnPropertyChanged(); } }
    public bool IsThumbInfoVisible { get => _isThumbInfoVisible; set { _isThumbInfoVisible = value; OnPropertyChanged(); } }
    public string Comment { get; set; } = string.Empty;
    public ObservableCollection<AppInfoRating> Rating { get; } = new ObservableCollection<AppInfoRating>();
    public AppMode AppMode { get => _appMode; set { _appMode = value; OnPropertyChanged(); } }
    public string Dimension { get; set; } = string.Empty;
    public string FullGeoName { get; set; } = string.Empty;
    public event PropertyChangedEventHandler PropertyChanged;

    public string FilePath {
      get {
        if (CurrentMediaItem == null) return string.Empty;
        if (AppMode == AppMode.Viewer && CurrentMediaItem.FolderKeyword != null) 
          return $"{CurrentMediaItem.FolderKeyword.FullPath}\\{CurrentMediaItem.Data.FileName}";
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
        for (var i = 0; i < _currentMediaItem?.Data.Rating; i++) 
          Rating.Add(new AppInfoRating {IconName = IconName.Star });

        Comment = _currentMediaItem == null ? string.Empty : _currentMediaItem.CommentEscaped;
        OnPropertyChanged($"Comment");

        Dimension = _currentMediaItem == null ? string.Empty : $"{_currentMediaItem.Data.Width}x{_currentMediaItem.Data.Height}";
        OnPropertyChanged($"Dimension");

        var aCore = (AppCore) Application.Current.Properties[nameof(AppProperty.AppCore)];
        FullGeoName = aCore.GeoNames.GetGeoNameHierarchy(_currentMediaItem?.Data.GeoNameId);
        OnPropertyChanged($"FullGeoName");
      }
    }

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
