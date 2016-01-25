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

    public void OnPropertyChanged(string name) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
