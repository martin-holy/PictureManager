namespace MH.Utils.BaseClasses {
  public class ListItem<T> : ObservableObject {
    private T _content;
    private bool _isSelected;
    private bool _isVisible = true;

    public T Content { get => _content; set { _content = value; OnPropertyChanged(); } }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }

    public ListItem(T content) {
      Content = content;
    }
  }
}
