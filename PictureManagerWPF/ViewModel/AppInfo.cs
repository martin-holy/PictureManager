using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PictureManager.ViewModel {
  public class AppInfo : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    private string _viewBaseInfo;
    public string ViewBaseInfo {
      get { return _viewBaseInfo; }
      set { _viewBaseInfo = value; OnPropertyChanged(); }
    }

    private string _currentPictureFilePath;
    public string CurrentPictureFilePath {
      get { return _currentPictureFilePath; }
      set { _currentPictureFilePath = value; OnPropertyChanged(); }
    }

    private bool _keywordsEditMode;
    public bool KeywordsEditMode {
      get { return _keywordsEditMode; }
      set { _keywordsEditMode = value; OnPropertyChanged(); }
    }

    private AppModes _appMode;
    public AppModes AppMode
    {
      get { return _appMode; }
      set {
        _appMode = value;
        OnPropertyChanged();
        var aCore = (AppCore) Application.Current.Properties[nameof(AppProps.AppCore)];
        if (aCore != null) {
          aCore.WMain.StatBarOkCancelPanel.Visibility = _appMode == AppModes.KeywordsEdit || _appMode == AppModes.ViewerEdit
            ? Visibility.Visible
            : Visibility.Collapsed;
          aCore.UpdateStatusBarInfo();
        }
      }
    }

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
