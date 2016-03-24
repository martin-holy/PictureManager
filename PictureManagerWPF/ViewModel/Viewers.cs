using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.ViewModel {
  public class Viewers : BaseTreeViewItem {
    public ObservableCollection<Viewer> Items { get; set; }
    public DataModel.PmDataContext Db;

    public Viewers() {
      Items = new ObservableCollection<Viewer>();
      Title = "Viewers";
      IconName = "appbar_eye";
    }

    public void Load() {
      Items.Clear();
      foreach (var viewer in Db.Viewers.OrderBy(x => x.Name).Select(x => new Viewer(Db, x))) {
        Items.Add(viewer);
      }
      IsExpanded = true;
    }
  }
}
