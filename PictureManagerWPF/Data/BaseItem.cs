using System.ComponentModel;

namespace PictureManager.Data {
  public class BaseItem:INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged(string name) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private bool _isExpanded;
    private bool _isSelected;
    private string _title;
    private string _iconName;

    public virtual bool IsExpanded { get { return _isExpanded; } set { _isExpanded = value; OnPropertyChanged("IsExpanded"); } }
    public bool IsSelected { get { return _isSelected; } set { _isSelected = value; OnPropertyChanged("IsSelected"); } }
    public string Title { get { return _title; } set { _title = value; OnPropertyChanged("Title"); } }
    public string IconName { get { return _iconName; } set { _iconName = value; OnPropertyChanged("IconName"); } }
  }
}
