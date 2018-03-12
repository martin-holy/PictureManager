namespace PictureManager.ViewModel {
  public sealed class Filters: BaseCategoryItem {

    public Filters() : base (Category.Filters) {
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
