using MH.UI.Interfaces;
using MH.Utils.Interfaces;

namespace MH.UI.Controls {
  public class CollectionViewRow<T> : CollectionViewItem<T>, ICollectionViewRow<T> where T : ISelectable {
    public CollectionViewRow(ICollectionViewGroup<T> parent) : base(parent) { }
  }
}
