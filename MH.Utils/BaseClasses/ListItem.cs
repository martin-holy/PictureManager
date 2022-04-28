using MH.Utils.Interfaces;

namespace MH.Utils.BaseClasses {
  public class ListItem : ObservableObject, IListItem {
    private bool _isSelected;
    private bool _isHidden;
    private string _iconName;
    private string _name;

    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public bool IsHidden { get => _isHidden; set { _isHidden = value; OnPropertyChanged(); } }
    public string IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }

    protected ListItem() { }

    protected ListItem(string iconName, string name) {
      IconName = iconName;
      Name = name;
    }
  }

  public class ListItem<T> : ListItem {
    private T _content;

    public T Content { get => _content; set { _content = value; OnPropertyChanged(); } }

    protected ListItem(T content) {
      Content = content;
    }

    protected ListItem(T content, string iconName, string name) : base(iconName, name) {
      Content = content;
    }
  }
}
