using MH.UI.Controls;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;

namespace MH.UI.Interfaces {
  public interface ICollectionView : INotifyPropertyChanged {
    public ExtObservableCollection<object> RootHolder { get; }
    public List<object> ScrollToItems { get; set; }
    public int ScrollToIndex { get; set; }
    public bool ScrollToTop { get; set; }
    public bool IsScrollUnitItem { get; set; }
    public bool IsSizeChanging { get; set; }
    public void OpenItem(object item);
    public void SelectItem(object row, object item, bool isCtrlOn, bool isShiftOn);
    public void SetExpanded(object group);
    public bool SetTopItem(object o);
  }

  public interface ICollectionViewItem<T> : ITreeItemBase<ICollectionViewGroup<T>, ICollectionViewItem<T>, T> where T : ISelectable { }

  public interface ICollectionViewGroup<T> : ICollectionViewItem<T> where T : ISelectable {
    public CollectionView<T> View { get; set; }
    public List<T> Source { get; }
    public CollectionViewGroupByItem<T>[] GroupByItems { get; set; }
    public CollectionViewGroupByItem<T> GroupedBy { get; set; }
    public double Width { get; set; }
    public string Title { get; set; }
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

  public interface ICollectionViewRow<T> : ICollectionViewItem<T> where T : ISelectable { }
}
