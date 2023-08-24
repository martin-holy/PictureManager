using MH.UI.Controls;
using MH.Utils.Interfaces;
using System.Collections.Generic;

namespace MH.UI.Interfaces {
  public interface ICollectionView : ITreeView, ITitled {
    public void OpenItem(object item);
    public void SelectItem(object row, object item, bool isCtrlOn, bool isShiftOn);
    public void SetExpanded(object group);
  }

  public interface ICollectionViewGroup<T> : ITreeItem where T : ISelectable {
    public CollectionView<T> View { get; set; }
    public List<T> Source { get; }
    public IEnumerable<ICollectionViewGroup<T>> Groups { get; }
    public CollectionViewGroupByItem<T>[] GroupByItems { get; set; }
    public CollectionViewGroupByItem<T> GroupedBy { get; set; }
    public double Width { get; set; }
    public bool IsRoot { get; set; }
    public bool IsRecursive { get; set; }
    public bool IsGroupBy { get; set; }
    public bool IsThenBy { get; set; }

    public void Clear();
    public void InsertItem(T item, ISet<ICollectionViewGroup<T>> toReWrap);
    public void RemoveItem(T item, ISet<ICollectionViewGroup<T>> toReWrap);
    public void ReWrap();
    public void UpdateGroupByItems(CollectionViewGroupByItem<T>[] newGroupByItems);
  }

  public interface ICollectionViewRow<T> : ILeafyTreeItem<T> where T : ISelectable { }
}
