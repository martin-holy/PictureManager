namespace PictureManager.Domain.Models {
  public class MediaItemsGroup : ObservableObject {
    private string _title;
    private bool _isTitleVisible;
    private Folder _folder;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public bool IsTitleVisible { get => _isTitleVisible; set { _isTitleVisible = value; OnPropertyChanged(); } }
    public Folder Folder { get => _folder; set { _folder = value; OnPropertyChanged(); } }
  }
}
