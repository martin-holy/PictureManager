using System.Collections.ObjectModel;

namespace PictureManager.Data {
  public class Filters: BaseItem {
    public ObservableCollection<Filter> Items { get; set; }
    public DbStuff Db;

    public Filters() {
      Items = new ObservableCollection<Filter>();
    }

    public void Load() {
      Items.Clear();
    }
  }
}
