using MH.Utils.Extensions;
using MH.Utils.Interfaces;

namespace MH.UI.Controls {
  public class CollectionViewRow<T> where T : ISelectable {
    public CollectionViewGroup<T> Group { get; }
    public ExtObservableCollection<T> Items { get; } = new();
    public bool IsExpanded { get; set; } = true;

    public CollectionViewRow(CollectionViewGroup<T> group) {
      Group = group;
    }
  }
}
