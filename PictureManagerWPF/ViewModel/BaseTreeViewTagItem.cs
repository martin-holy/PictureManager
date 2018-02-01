namespace PictureManager.ViewModel {
  public class BaseTreeViewTagItem : BaseTreeViewItem {
    private bool _isMarked;
    private int _picCount;

    public bool IsMarked { get => _isMarked; set { _isMarked = value; OnPropertyChanged(); } }
    public int PicCount { get => _picCount; set { _picCount = value; OnPropertyChanged(); } }
    public int Id;
  }
}
