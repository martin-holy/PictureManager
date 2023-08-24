using MH.Utils.Extensions;

namespace MH.Utils.BaseClasses {
  public class LeafyTreeItem<T> : TreeItem {
    public ExtObservableCollection<T> Leaves { get; set; } = new();
  }
}
