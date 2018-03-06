using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PictureManager.ViewModel {
  public class AppInfoRating {
    public string IconName { get; set; }
  }

  public class AppInfo : INotifyPropertyChanged {
    private int _selected;
    private int _modifed;
    private int _progressBarValue;
    private string _positionSlashCount;
    private BaseMediaItem _currentMediaItem;
    private AppModes _appMode;

    public int Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public int Modifed { get => _modifed; set { _modifed = value; OnPropertyChanged(); } }
    public int ProgressBarValue { get => _progressBarValue; set { _progressBarValue = value; OnPropertyChanged(); } }
    public string PositionSlashCount { get => _positionSlashCount; set { _positionSlashCount = value; OnPropertyChanged(); } }
    public string Comment { get; set; } = string.Empty;
    public ObservableCollection<AppInfoRating> Rating { get; set; } = new ObservableCollection<AppInfoRating>();
    public AppModes AppMode { get => _appMode; set { _appMode = value; OnPropertyChanged(); } }
    public event PropertyChangedEventHandler PropertyChanged;

    public string FilePath {
      get {
        if (CurrentMediaItem == null) return string.Empty;
        if (AppMode == AppModes.Viewer && CurrentMediaItem.FolderKeyword != null) 
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
        Comment = string.Empty;

        if (_currentMediaItem == null) return;

        for (var i = 0; i < _currentMediaItem.Data.Rating; i++) 
          Rating.Add(new AppInfoRating {IconName = "appbar_star" });

        Comment = _currentMediaItem.CommentEscaped;
        OnPropertyChanged($"Comment");
      }
    }

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
