using System.ComponentModel;
using System.Runtime.CompilerServices;

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

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
