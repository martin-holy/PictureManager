namespace PictureManager.ViewModel {
  public class Filters: BaseTreeViewItem {

    public Filters() {
      Title = "Filters";
      IconName = "appbar_filter";
    }

    public void Load() {
      var f = new Filter();
      f.GetSubFilters(false, Items, null);
      IsExpanded = true;
    }
  }
}
