using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PictureManager.ViewModel {
  public class AppInfoRating {
    public string IconName { get; set; }
  }

  public class AppInfo : INotifyPropertyChanged {
    private int _selected;
    private int _modifed;
    private string _positionSlashCount;
    private BaseMediaItem _currentMediaItem;


    public int Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public int Modifed { get => _modifed; set { _modifed = value; OnPropertyChanged(); } }
    public string PositionSlashCount { get => _positionSlashCount; set { _positionSlashCount = value; OnPropertyChanged(); } }
    public string Comment { get; set; } = string.Empty;
    public ObservableCollection<AppInfoRating> Rating { get; set; } = new ObservableCollection<AppInfoRating>();

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


    public event PropertyChangedEventHandler PropertyChanged;

    private string _viewBaseInfo;
    public string ViewBaseInfo {
      get => _viewBaseInfo;
      set { _viewBaseInfo = value; OnPropertyChanged(); }
    }

    private string _currentPictureFilePath;
    public string CurrentPictureFilePath {
      get => _currentPictureFilePath;
      set { _currentPictureFilePath = value; OnPropertyChanged(); }
    }

    private bool _keywordsEditMode;
    public bool KeywordsEditMode {
      get => _keywordsEditMode;
      set { _keywordsEditMode = value; OnPropertyChanged(); }
    }

    private AppModes _appMode;
    public AppModes AppMode
    {
      get => _appMode;
      set {
        _appMode = value;
        OnPropertyChanged();
        var aCore = (AppCore) Application.Current.Properties[nameof(AppProps.AppCore)];
        if (aCore == null) return;
        aCore.WMain.StatBarOkCancelPanel.Visibility = _appMode == AppModes.KeywordsEdit || _appMode == AppModes.ViewerEdit
          ? Visibility.Visible
          : Visibility.Collapsed;
        aCore.UpdateStatusBarInfo();
      }
    }

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
