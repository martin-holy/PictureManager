namespace PictureManager.ViewModel {
  public class Filters: BaseCategoryItem {

    public Filters() : base (Categories.Filters) {
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
