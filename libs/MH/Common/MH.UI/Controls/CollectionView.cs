﻿using MH.UI.Dialogs;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls;

public abstract class CollectionView<T> : TreeView<ITreeItem>, ICollectionView where T : class, ISelectable {
  private readonly HashSet<CollectionViewGroup<T>> _groupByItemsRoots = [];
  private readonly GroupByDialog<T> _groupByDialog = new();
  private readonly HashSet<T> _pendingRemove = [];
  private readonly HashSet<T> _pendingUpdate = [];

  public CollectionViewGroup<T>? Root { get; set; }
  public T? TopItem { get; set; }
  public T? LastSelectedItem { get; set; }
  public CollectionViewGroup<T>? TopGroup { get; set; }
  public CollectionViewRow<T>? LastSelectedRow { get; set; }
  public object? UIView { get; set; }
  public bool AddInOrder { get; set; } = true;
  public bool CanOpen { get; set; } = true;
  public bool CanSelect { get; set; } = true;
  public bool IsMultiSelect { get; set; } = true;
  public int GroupContentOffset { get; set; } = 0;
  public string Icon { get; set; }
  public string Name { get; set; }

  public RelayCommand<CollectionViewGroup<T>> OpenGroupByDialogCommand { get; }

  public event EventHandler<ObjectEventArgs<T>> ItemOpenedEvent = delegate { };
  public event EventHandler<SelectionEventArgs<T>> ItemSelectedEvent = delegate { };

  protected CollectionView(string icon, string name) {
    Icon = icon;
    Name = name;
    OpenGroupByDialogCommand = new(OpenGroupByDialog, null, "Group by");
  }

  protected void RaiseItemOpened(T item) => ItemOpenedEvent(this, new(item));
  protected void RaiseItemSelected(SelectionEventArgs<T> args) => ItemSelectedEvent(this, args);

  public abstract int GetItemSize(T item, bool getWidth);
  public abstract IEnumerable<GroupByItem<T>> GetGroupByItems(IEnumerable<T> source);
  public abstract int SortCompare(T itemA, T itemB);
  public virtual void OnItemOpened(T item) { }
  public virtual void OnItemSelected(SelectionEventArgs<T> args) { }

  public void OpenItem(object? item) {
    if (item is not T i) return;
    RaiseItemOpened(i);
    OnItemOpened(i);
  }

  public void SelectItem(object row, object item, bool isCtrlOn, bool isShiftOn) {
    if (row is not CollectionViewRow<T> r || item is not T i) return;
    if (!IsMultiSelect) { isCtrlOn = false; isShiftOn = false; }
    LastSelectedItem = i;
    LastSelectedRow = r;
    var args = new SelectionEventArgs<T>(((CollectionViewGroup<T>)r.Parent!).Source, i, isCtrlOn, isShiftOn);
    RaiseItemSelected(args);
    OnItemSelected(args);
  }

  public void Reload(List<T> source, GroupMode groupMode, GroupByItem<T>[]? groupByItems, bool expandAll, bool removeEmpty = true) {
    var root = new CollectionViewGroup<T>(this, source, new(new ListItem(Icon, Name, this), null)) {
      IsGroupingRoot = true,
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
    _groupByItemsRoots.Add(Root!);
  }

  public void ReWrapAll() {
    if (Root == null) return;
    UpdateRoot(Root, _ => CollectionViewGroup<T>.ReWrapAll(Root));
    ScrollTo(TopGroup, TopItem);
  }

  public void ReWrapAll(IEnumerable<T> items) {
    if (Root?.Source.Intersect(items).Any() == true) ReWrapAll();
  }

  public void Insert(T item) =>
    Insert([item]);

  public void Insert(T[] items) =>
    ReGroupItems(items, false, false);

  public void Update(T item) =>
    Update([item]);

  public void Update(T[] items) =>
    ReGroupItems(items, false, true);

  public void Remove(T item) =>
    Remove([item]);

  public void Remove(T[] items) =>
    ReGroupItems(items, true, true);

  public void ReGroupPendingItems() {
    ReGroupItems(_pendingRemove.ToArray(), true, false);
    ReGroupItems(_pendingUpdate.Except(_pendingRemove).ToArray(), false, false);
    _pendingRemove.Clear();
    _pendingUpdate.Clear();
  }

  private void ReGroupItems(T[]? items, bool remove, bool ifContains) {
    if (Root == null || items == null || items.Length == 0) return;

    if (!IsVisible && remove) {
      items.ForEach(x => _pendingRemove.Add(x));
      return;
    }

    if (ifContains) items = Root.Source.Intersect(items).ToArray();
    if (items.Length == 0) return;
    
    if (!IsVisible && !remove) {
      items.ForEach(x => _pendingUpdate.Add(x));
      return;
    }
    
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

  public void RemoveEmptyGroups(CollectionViewGroup<T> group, ISet<CollectionViewGroup<T>>? toReWrap) {
    var removedGroups = new List<CollectionViewGroup<T>>();
    toReWrap ??= new HashSet<CollectionViewGroup<T>>();
    CollectionViewGroup<T>.RemoveEmptyGroups(group, toReWrap, removedGroups);
    if (TopGroup != null && removedGroups.Contains(TopGroup)) TopGroup = null;
    if (toReWrap.Count == 0) return;
    foreach (var g in toReWrap) g.ReWrap();
    if (toReWrap.Any(x => x.IsFullyExpanded()))
      ScrollTo(TopGroup ?? Root, TopItem);
  }

  public override void OnIsVisibleChanged() {
    if (!IsVisible) return;
    ReGroupPendingItems();
    ScrollTo(TopGroup, TopItem);
  }

  public void SetExpanded(object group) {
    if (Root == null || group is not CollectionViewGroup<T> g) return;
      
    UpdateRoot(Root, _ => g.SetExpanded<CollectionViewGroup<T>>(g.IsExpanded));
    TopItem = default;
    TopGroup = g;
    ScrollTo(TopGroup, TopItem);
  }

  private void OpenGroupByDialog(CollectionViewGroup<T>? group) {
    if (group != null && _groupByDialog.Open(group, GetGroupByItems(group.Source)))
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
      TopGroup = (CollectionViewGroup<T>)row.Parent!;
      if (row.Leaves.Count > 0)
        TopItem = Selecting<T>.GetNotSelectedItem(TopGroup.Source, row.Leaves[0]);
    }
  }

  public void ScrollTo(CollectionViewGroup<T>? group, T? item, bool exactly = true) {
    if (Root == null || (group == null && item == null)) return;

    CollectionViewRow<T>? row = default;

    if (item != null)
      CollectionViewGroup<T>.FindItem(group ?? Root, item, ref group, ref row);

    TopGroup = group;
    TopItem = item;
    ScrollTo(row != null ? row : group ?? Root, exactly);
  }

  public T? SelectFirstItem() {
    if ((Root?.GetNextBranchEndOfType() ?? Root) is not { } group) return default;
    if (group.GetItemByIndex(0) is not { } item) return default;
    if (group.GetRowWithItem(item) is not { } row) return default;
    SelectItem(row, item, false, false);
    return item;
  }

  public T? SelectNextItem(bool inGroup, bool first) {
    if (first || LastSelectedItem == null || LastSelectedRow == null) return SelectFirstItem();
    if (LastSelectedRow.Parent is not CollectionViewGroup<T> group) return default;
    var index = group.Source.IndexOf(LastSelectedItem);
    var item = group.GetItemByIndex(index + 1);
    CollectionViewGroup<T>? itemGroup = group;

    if (item == null)
      if (inGroup) item = group.Source[0];
      else {
        itemGroup = group.GetNextBranchEndOfType();
        if (itemGroup != null)
          item = itemGroup.GetItemByIndex(0);
      }

    if (item == null || itemGroup?.GetRowWithItem(item) is not { } row) return default;
    SelectItem(row, item, false, false);
    return item;
  }

  public void SelectNextOrFirstItem(bool inGroup, bool first) {
    var item = SelectNextItem(inGroup, first);
    if (item == null) SelectNextItem(inGroup, true);
  }
}