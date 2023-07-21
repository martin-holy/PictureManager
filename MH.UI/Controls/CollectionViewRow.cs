using MH.Utils.Extensions;
using System.Collections.ObjectModel;

namespace MH.UI.Controls {
  public class CollectionViewRow<T> {
    public CollectionViewGroup<T> Group { get; }
    public ExtObservableCollection<T> Items { get; } = new();
    public bool IsExpanded { get; set; } = true;

    public CollectionViewRow(CollectionViewGroup<T> group) {
      Group = group;
    }
  }
}
