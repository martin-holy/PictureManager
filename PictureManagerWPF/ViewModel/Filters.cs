namespace PictureManager.ViewModel {
  public class Filters: BaseTreeViewItem {
    public DataModel.PmDataContext Db;

    public Filters() {
      Title = "Filters";
      IconName = "appbar_filter";
    }

    public void Load() {
      Filter.GetSubFilters(false, Items, null, Db);
      IsExpanded = true;
    }
  }
}
