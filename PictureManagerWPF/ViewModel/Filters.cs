using System.Collections.ObjectModel;

namespace PictureManager.ViewModel {
  public class Filters: BaseTreeViewItem {
    public ObservableCollection<Filter> Items { get; set; }
    public DataModel.PmDataContext Db;

    public Filters() {
      Items = new ObservableCollection<Filter>();
      Title = "Filters";
      IconName = "appbar_filter";
    }

    public void Load() {
      Filter.GetSubFilters(false, Items, null, Db);
      IsExpanded = true;
    }
  }
}
