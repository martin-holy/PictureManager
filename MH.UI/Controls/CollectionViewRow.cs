using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace MH.UI.Controls {
  public class CollectionViewRow<T> : LeafyTreeItem<T>, ICollectionViewRow<T> where T : ISelectable { }
}
