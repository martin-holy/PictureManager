using System.Collections.ObjectModel;

namespace PictureManager.Data {
  public class Filter: BaseTagItem {
    public ObservableCollection<Filter> Items { get; set; }
    public Filter Parent;

    public Filter() {
      Items = new ObservableCollection<Filter>();
    }
  }
}
