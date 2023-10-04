using MH.UI.Dialogs;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls;

public abstract class CollectionView<T> : TreeView<ITreeItem>, ICollectionView where T : ISelectable {
  private T _topItem;
  private CollectionViewGroup<T> _topGroup;
  private readonly HashSet<CollectionViewGroup<T>> _groupByItemsRoots = new();
  private readonly GroupByDialog<T> _groupByDialog = new();

  public CollectionViewGroup<T> Root { get; set; }
  public T TopItem { get; set; }
  public T LastSelectedItem { get; set; }
  public CollectionViewGroup<T> TopGroup { get; set; }
  public CollectionViewRow<T> LastSelectedRow { get; set; }
  public bool SelectionDisabled { get; set; }
  public int GroupContentOffset { get; set; } = 0;
  public string Icon { get; set; }
  public string Name { get; set; }

  public RelayCommand<CollectionViewGroup<T>> OpenGroupByDialogCommand { get; }

  protected CollectionView() {
    OpenGroupByDialogCommand = new(OpenGroupByDialog);
  }

  public abstract int GetItemWidth(T item);
  public abstract IEnumerable<CollectionViewGroupByItem<T>> GetGroupByItems(IEnumerable<T> source);
  public abstract int SortCompare(T itemA, T itemB);
  public virtual void OnOpenItem(T item) { }
  public virtual void OnSelectItem(IEnumerable<T> source, T item, bool isCtrlOn, bool isShiftOn) { }

  public void OpenItem(object item) {
    if (item is T i) OnOpenItem(i);
  }

  // TODO BUG not always unable to select, ok after reload 
  public void SelectItem(object row, object item, bool isCtrlOn, bool isShiftOn) {
    if (SelectionDisabled || row is not CollectionViewRow<T> r || item is not T i) return;
    LastSelectedItem = i;
    LastSelectedRow = r;
    OnSelectItem(((CollectionViewGroup<T>)r.Parent).Source, i, isCtrlOn, isShiftOn);
  }

  public void Reload(List<T> source, GroupMode groupMode, CollectionViewGroupByItem<T>[] groupByItems, bool expandAll, bool removeEmpty = true) {
    var root = new CollectionViewGroup<T>(source) {
      View = this,
      IsGroupingRoot = true,
      GroupedBy = new(new ListItem(Icon, Name, this), null),
      IsGroupBy = groupMode is GroupMode.GroupBy or GroupMode.GroupByRecursive,
      IsThenBy = groupMode is GroupMode.ThenBy or GroupMode.ThenByRecursive,
      IsRecursive = groupMode is GroupMode.GroupByRecursive or GroupMode.ThenByRecursive,
      GroupByItems = groupByItems?.Length == 0 ? null : groupByItems
    };

    TopGroup = null;
    TopItem = default;
    ScrollToTopAction?.Invoke();
    UpdateRoot(root, _ => {
      Root = root;
      Root.GroupIt();
      if (removeEmpty) CollectionViewGroup<T>.RemoveEmptyGroups(Root, null, null);
      if (expandAll) Root.SetExpanded<CollectionViewGroup<T>>(true);
    });

    _groupByItemsRoots.Clear();
    _groupByItemsRoots.Add(Root);
  }

  public void ReWrapAll() {
    UpdateRoot(Root, _ => CollectionViewGroup<T>.ReWrapAll(Root));
  }

  public void ReGroupItems(T[] items, bool remove) {
    if (Root == null || items == null) return;

    var toReWrap = new HashSet<CollectionViewGroup<T>>();

    if (remove)
      foreach (var item in items)
        Root.RemoveItem(item, toReWrap);
    else {
      foreach (var gbiRoot in _groupByItemsRoots)
        gbiRoot.UpdateGroupByItems(GetGroupByItems(items).ToArray());

      foreach (var item in items)
        Root.InsertItem(item, toReWrap);
    }

    RemoveEmptyGroups(Root, toReWrap);
  }

  public void RemoveEmptyGroups(CollectionViewGroup<T> group, ISet<CollectionViewGroup<T>> toReWrap) {
    var removedGroups = new List<CollectionViewGroup<T>>();
    toReWrap ??= new HashSet<CollectionViewGroup<T>>();
    CollectionViewGroup<T>.RemoveEmptyGroups(group, toReWrap, removedGroups);
    if (removedGroups.Contains(TopGroup)) TopGroup = null;
    if (toReWrap.Count == 0) return;
    foreach (var g in toReWrap) g.ReWrap();
    if (toReWrap.Any(x => x.IsFullyExpanded()))
      ScrollTo(TopGroup ?? Root, TopItem);
  }

  public override void OnSizeChanging(bool value) {
    if (value) {
      _topItem = TopItem;
      _topGroup = TopGroup;
    }
    else {
      TopItem = _topItem;
      TopGroup = _topGroup;
      ScrollTo(TopGroup, TopItem);
    }
  }

  public void SetExpanded(object group) {
    if (group is not CollectionViewGroup<T> g) return;
      
    UpdateRoot(Root, _ => g.SetExpanded<CollectionViewGroup<T>>(g.IsExpanded));
    TopItem = default;
    TopGroup = g;
    ScrollTo(TopGroup, TopItem);
  }

  private void OpenGroupByDialog(CollectionViewGroup<T> group) {
    if (_groupByDialog.Open(group, GetGroupByItems(group.Source)))
      _groupByItemsRoots.Add(group);
  }

  public override void OnTopTreeItemChanged() {
    var row = TopTreeItem as CollectionViewRow<T>;
    var group = TopTreeItem as CollectionViewGroup<T>;

    TopItem = default;
    TopGroup = null;

    if (group != null)
      TopGroup = group;
    else if (row != null) {
      TopGroup = (CollectionViewGroup<T>)row.Parent;
      if (row.Leaves.Count > 0)
        TopItem = Selecting<T>.GetNotSelectedItem(TopGroup.Source, row.Leaves[0]);
    }
  }

  public void ScrollTo(CollectionViewGroup<T> group, T item) {
    if (group == null && item == null) return;

    CollectionViewRow<T> row = default;

    if (item != null)
      CollectionViewGroup<T>.FindItem(group, item, ref group, ref row);

    TopGroup = group;
    TopItem = item;
    ScrollTo(row != null ? row : group);
  }
}