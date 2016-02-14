using System.ComponentModel;

namespace PictureManager.Data {
  public class AppInfo : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    private string _viewBaseInfo;
    public string ViewBaseInfo {
      get { return _viewBaseInfo; }
      set { _viewBaseInfo = value; OnPropertyChanged("ViewBaseInfo"); }
    }

    private string _currentPictureFilePath;
    public string CurrentPictureFilePath {
      get { return _currentPictureFilePath; }
      set { _currentPictureFilePath = value; OnPropertyChanged("CurrentPictureFilePath"); }
    }

    private bool _keywordsEditMode;
    public bool KeywordsEditMode {
      get { return _keywordsEditMode; }
      set { _keywordsEditMode = value; OnPropertyChanged("KeywordsEditMode"); }
    }

    public void OnPropertyChanged(string name) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
