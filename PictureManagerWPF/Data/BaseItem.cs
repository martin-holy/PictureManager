using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PictureManager.Data {
  public class BaseItem:INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private bool _isExpanded;
    private bool _isSelected;
    private bool _isTitleEdited;
    private string _title;
    private string _iconName;

    public virtual bool IsExpanded { get { return _isExpanded; } set { _isExpanded = value; OnPropertyChanged(); } }
    public bool IsSelected { get { return _isSelected; } set { _isSelected = value; OnPropertyChanged(); } }
    public bool IsTitleEdited { get { return _isTitleEdited; } set { _isTitleEdited = value; OnPropertyChanged(); } }
    public string Title { get { return _title; } set { _title = value; OnPropertyChanged(); } }
    public string IconName { get { return _iconName; } set { _iconName = value; OnPropertyChanged(); } }
  }
}
