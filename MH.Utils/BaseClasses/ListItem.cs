namespace MH.Utils.BaseClasses {
  public class ListItem<T> : ObservableObject {
    private T _content;
    private bool _isSelected;
    private bool _isHidden = true;

    public T Content { get => _content; set { _content = value; OnPropertyChanged(); } }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public bool IsHidden { get => _isHidden; set { _isHidden = value; OnPropertyChanged(); } }

    public ListItem(T content) {
      Content = content;
    }
  }
}
