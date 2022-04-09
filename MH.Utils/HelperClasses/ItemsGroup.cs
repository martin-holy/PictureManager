using System.Collections.ObjectModel;

namespace MH.Utils.HelperClasses {
  public class ItemsGroup {
    public ObservableCollection<object> Info { get; } = new();
    public ObservableCollection<object> Items { get; } = new();
  }
}
