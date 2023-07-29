using MH.UI.Dialogs;
using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls {
  public abstract class CollectionView<T> : ObservableObject, ICollectionView where T : ISelectable {
    private List<object> _scrollToItems;
    private bool _scrollToTop;
    private bool _isSizeChanging;
    private T _topItem;
    private CollectionViewGroup<T> _topGroup;
    private readonly HashSet<CollectionViewGroup<T>> _groupByItemsRoots = new();
    private readonly GroupByDialog<T> _groupByDialog = new();

    public ExtObservableCollection<object> RootHolder { get; } = new();
    public CollectionViewGroup<T> Root { get; set; }
    public T TopItem { get; set; }
    public T LastSelectedItem { get; set; }
    public CollectionViewGroup<T> TopGroup { get; set; }
    public CollectionViewRow<T> LastSelectedRow { get; set; }
    public List<object> ScrollToItems { get => _scrollToItems; set { _scrollToItems = value; OnPropertyChanged(); } }
    public bool ScrollToTop { get => _scrollToTop; set { _scrollToTop = value; OnPropertyChanged(); } }
    public bool IsSizeChanging { get => _isSizeChanging; set => OnSizeChanging(value); }

    public RelayCommand<object> ExpandAllCommand { get; }
    public RelayCommand<object> CollapseAllCommand { get; }
    public RelayCommand<CollectionViewGroup<T>> OpenGroupByDialogCommand { get; }

    protected CollectionView() {
      ExpandAllCommand = new(_ => SetExpanded(TopGroup, true), _ => TopGroup != null);
      CollapseAllCommand = new(_ => SetExpanded(TopGroup, false), _ => TopGroup != null);
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

    public void SelectItem(object row, object item, bool isCtrlOn, bool isShiftOn) {
      if (row is not CollectionViewRow<T> r || item is not T i) return;
      LastSelectedItem = i;
      LastSelectedRow = r;
      OnSelectItem(r.Group.Source, i, isCtrlOn, isShiftOn);
    }

    public void Update(Action<IList<object>> itemsAction) {
      if (itemsAction == null) return;
      RootHolder.Execute(items => {
        items.Clear();
        itemsAction(items);
        items.Add(Root);
      });
    }

    public void SetRoot(CollectionViewGroup<T> root, bool expandAll) {
      ScrollToTop = true;
      Update(_ => {
        Root = root;
        CollectionViewGroup<T>.GroupIt(Root);
        CollectionViewGroup<T>.RemoveEmptyGroups(Root, null);
        if (expandAll) Root.SetExpanded(true);
      });

      _groupByItemsRoots.Clear();
      _groupByItemsRoots.Add(Root);
    }

    public void ReWrapAll() {
      Update(_ => CollectionViewGroup<T>.ReWrapAll(Root));
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
      toReWrap ??= new HashSet<CollectionViewGroup<T>>();
      CollectionViewGroup<T>.RemoveEmptyGroups(group, toReWrap);
      if (toReWrap.Count == 0) return;
      foreach (var g in toReWrap) g.ReWrap();
      if (toReWrap.Any(CollectionViewGroup<T>.IsFullyExpanded)) ScrollToTopItem();
    }

    private void OnSizeChanging(bool value) {
      _isSizeChanging = value;

      if (value) {
        _topItem = TopItem;
        _topGroup = TopGroup;
      }
      else {
        TopItem = _topItem;
        TopGroup = _topGroup;
        ScrollToTopItem();
      }
    }

    public void SetExpanded(object group) {
      if (group is not CollectionViewGroup<T> g) return;
      
      Update(_ => g.SetExpanded(g.IsExpanded));
      TopItem = default;
      TopGroup = g;
      ScrollToTopItem();
    }

    public void SetExpanded(CollectionViewGroup<T> group, bool value) =>
      Update(_ => group.SetExpanded(value));

    private void OpenGroupByDialog(CollectionViewGroup<T> group) {
      if (_groupByDialog.Open(group, GetGroupByItems(group.Source)))
        _groupByItemsRoots.Add(group);
    }

    public bool SetTopItem(object o) {
      var row = o as CollectionViewRow<T>;
      var group = o as CollectionViewGroup<T>;

      TopItem = default;
      TopGroup = null;

      if (group != null)
        TopGroup = group;
      else if (row != null) {
        TopGroup = row.Group;
        if (row.Items.Count > 0)
          TopItem = Selecting<T>.GetNotSelectedItem(TopGroup.Source, row.Items[0]);
      }

      return row != null || group != null;
    }

    public void ScrollToTopItem() {
      if (TopItem == null && TopGroup == null) return;

      CollectionViewGroup<T> group = TopGroup;
      CollectionViewRow<T> row = null;

      if (TopItem != null)
        CollectionViewGroup<T>.FindItem(TopGroup ?? Root, TopItem, ref group, ref row);

      ScrollToItems = GetItemBranch(group, row);
    }

    public void ScrollToItem(T item) {
      CollectionViewGroup<T> group = null;
      CollectionViewRow<T> row = null;
      if (!CollectionViewGroup<T>.FindItem(Root, item, ref group, ref row)) return;
      ScrollToItems = GetItemBranch(group, row);
    }

    private static List<object> GetItemBranch(CollectionViewGroup<T> group, CollectionViewRow<T> row) {
      if (group == null) return null;
      var items = new List<object>();
      
      if (row != null)
        items.Add(row);

      items.Add(group);
      group = group.Parent;

      while (group != null) {
        items.Add(group);
        group.IsExpanded = true;
        group = group.Parent;
      }

      items.Reverse();

      return items;
    }
  }
}
