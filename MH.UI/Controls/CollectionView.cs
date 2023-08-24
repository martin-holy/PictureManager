using MH.UI.Dialogs;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls {
  public abstract class CollectionView<T> : TreeView, ICollectionView where T : ISelectable {
    private T _topItem;
    private ICollectionViewGroup<T> _topGroup;
    private readonly HashSet<ICollectionViewGroup<T>> _groupByItemsRoots = new();
    private readonly GroupByDialog<T> _groupByDialog = new();

    public ICollectionViewGroup<T> Root { get; set; }
    public T TopItem { get; set; }
    public T LastSelectedItem { get; set; }
    public ICollectionViewGroup<T> TopGroup { get; set; }
    public ICollectionViewRow<T> LastSelectedRow { get; set; }
    public bool SelectionDisabled { get; set; }
    public int GroupContentOffset { get; set; } = 0;
    public string GetTitle { get; set; }

    public RelayCommand<ICollectionViewGroup<T>> OpenGroupByDialogCommand { get; }

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
      if (SelectionDisabled || row is not ICollectionViewRow<T> r || item is not T i) return;
      LastSelectedItem = i;
      LastSelectedRow = r;
      OnSelectItem(((ICollectionViewGroup<T>)r.Parent).Source, i, isCtrlOn, isShiftOn);
    }

    public void Update(Action<IList<object>> itemsAction) {
      if (itemsAction == null) return;
      RootHolder.Execute(items => {
        items.Clear();
        itemsAction(items);
        items.Add(Root);
      });
    }

    public void SetRoot(ICollectionViewGroup<T> root, bool expandAll, bool removeEmpty = true) {
      TopGroup = null;
      TopItem = default;
      ScrollToTop = true;
      Update(_ => {
        Root = root;
        CollectionViewGroup<T>.GroupIt(Root);
        if (removeEmpty) CollectionViewGroup<T>.RemoveEmptyGroups(Root, null, null);
        if (expandAll) Root.SetExpanded<ICollectionViewGroup<T>>(true);
      });

      _groupByItemsRoots.Clear();
      _groupByItemsRoots.Add(Root);
    }

    public void ReWrapAll() {
      Update(_ => CollectionViewGroup<T>.ReWrapAll(Root));
    }

    public void ReGroupItems(T[] items, bool remove) {
      if (Root == null || items == null) return;

      var toReWrap = new HashSet<ICollectionViewGroup<T>>();

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

    public void RemoveEmptyGroups(ICollectionViewGroup<T> group, ISet<ICollectionViewGroup<T>> toReWrap) {
      var removedGroups = new List<ICollectionViewGroup<T>>();
      toReWrap ??= new HashSet<ICollectionViewGroup<T>>();
      CollectionViewGroup<T>.RemoveEmptyGroups(group, toReWrap, removedGroups);
      if (removedGroups.Contains(TopGroup)) TopGroup = null;
      if (toReWrap.Count == 0) return;
      foreach (var g in toReWrap) g.ReWrap();
      if (toReWrap.Any(x => x.IsFullyExpanded()))
        ScrollTo(TopGroup ?? Root, TopItem);
    }

    public override void OnSizeChanging(bool value) {
      base.OnSizeChanging(value);

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
      if (group is not ICollectionViewGroup<T> g) return;
      
      Update(_ => g.SetExpanded<ICollectionViewGroup<T>>(g.IsExpanded));
      TopItem = default;
      TopGroup = g;
      ScrollTo(TopGroup, TopItem);
    }

    private void OpenGroupByDialog(ICollectionViewGroup<T> group) {
      if (_groupByDialog.Open(group, GetGroupByItems(group.Source)))
        _groupByItemsRoots.Add(group);
    }

    public override bool SetTopItem(object o) {
      var row = o as ICollectionViewRow<T>;
      var group = o as ICollectionViewGroup<T>;

      TopItem = default;
      TopGroup = null;

      if (group != null)
        TopGroup = group;
      else if (row != null) {
        TopGroup = (ICollectionViewGroup<T>)row.Parent;
        if (row.Leaves.Count > 0)
          TopItem = Selecting<T>.GetNotSelectedItem(TopGroup.Source, row.Leaves[0]);
      }

      return row != null || group != null;
    }

    public void ScrollTo(ICollectionViewGroup<T> group, T item) {
      if (group == null && item == null) return;

      ICollectionViewRow<T> row = default;

      if (item != null)
        CollectionViewGroup<T>.FindItem(group, item, ref group, ref row);

      TopGroup = group;
      TopItem = item;
      ScrollTo(row != null ? row : group);
    }
  }
}
