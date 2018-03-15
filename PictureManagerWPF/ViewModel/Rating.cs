namespace PictureManager.ViewModel {
  public class Rating : BaseTreeViewTagItem {
    public int Value { get; set; }

    public Rating() {
      IconName = IconName.Star;
    }
  }
}
