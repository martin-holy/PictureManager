namespace PictureManager.ViewModel {
  public class FavoriteFolder : BaseTreeViewItem {
    public string FullPath { get; set; }

    public FavoriteFolder() {
      IconName = "appbar_folder";
    }
  }
}
