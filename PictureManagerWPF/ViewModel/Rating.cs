namespace PictureManager.ViewModel {
  public class Rating : BaseTreeViewTagItem {
    public int Value { get; set; }

    private bool _isChosen;
    public bool IsChosen { get { return _isChosen; } set { _isChosen = value; OnPropertyChanged(); } }
  }
}
