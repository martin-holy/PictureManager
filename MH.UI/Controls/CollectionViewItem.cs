using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace MH.UI.Controls {
  public class CollectionViewItem<T> : TreeItemBase<ICollectionViewGroup<T>, ICollectionViewItem<T>, T>, ICollectionViewItem<T> where T : ISelectable {
    public CollectionViewItem(ICollectionViewGroup<T> parent) : base(parent) { }
  }
}
