using System.Collections.ObjectModel;

namespace MH.UI.Controls {
  public class CollectionViewRow<T> {
    public CollectionViewGroup<T> Group { get; }
    public ObservableCollection<T> Items { get; } = new();
    public bool IsExpanded { get; set; } = true;

    public CollectionViewRow(CollectionViewGroup<T> group) {
      Group = group;
    }
  }
}
