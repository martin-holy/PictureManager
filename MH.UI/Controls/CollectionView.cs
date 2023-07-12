using MH.UI.Dialogs;
using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls {
  public class CollectionView<T> : ObservableObject, ICollectionView {
    private List<object> _scrollToItem;
    private bool _isSizeChanging;
    private T _topItem;
    private CollectionViewGroup<T> _topGroup;
    private readonly List<CollectionViewGroup<T>> _groupByItemsRoots = new();
    private readonly GroupByDialog<T> _groupByDialog = new();

    public ExtObservableCollection<object> RootHolder { get; } = new();
    public CollectionViewGroup<T> Root { get; set; }
    public T TopItem { get; set; }
    public T LastSelectedItem { get; set; }
    public CollectionViewGroup<T> TopGroup { get; set; }
    public CollectionViewRow<T> LastSelectedRow { get; set; }
    public List<object> ScrollToItem { get => _scrollToItem; set { _scrollToItem = value; OnPropertyChanged(); } }
    public bool IsSizeChanging { get => _isSizeChanging; set => OnSizeChanging(value); }

    public RelayCommand<CollectionViewGroup<T>> ExpandAllCommand { get; }
    public RelayCommand<CollectionViewGroup<T>> OpenGroupByDialogCommand { get; }

    public CollectionView() {
      ExpandAllCommand = new(ExpandAll);
      OpenGroupByDialogCommand = new(OpenGroupByDialog);
    }

    public virtual int GetItemWidth(object item) => throw new NotImplementedException();
    public virtual IEnumerable<CollectionViewGroupByItem<T>> GetGroupByItems(IEnumerable<T> source) => throw new NotImplementedException();
    public virtual string ItemOrderBy(T item) => throw new NotImplementedException();
    public virtual void Select(IEnumerable<T> source, T item, bool isCtrlOn, bool isShiftOn) => throw new NotImplementedException();

    public void Select(object row, object item, bool isCtrlOn, bool isShiftOn) {
      if (row is not CollectionViewRow<T> r || item is not T i) return;
      LastSelectedItem = i;
      LastSelectedRow = r;
      Select(r.Group.Source, i, isCtrlOn, isShiftOn);
    }

    // TODO scroll to top
    public void SetRoot(CollectionViewGroup<T> root, bool expandAll) {
      RootHolder.Execute(items => {
        items.Clear();
        Root = root;
        Root.GroupIt();
        if (expandAll) Root.ExpandAll();
        items.Add(Root);
      });

      _groupByItemsRoots.Clear();
      _groupByItemsRoots.Add(Root);
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

      foreach (var group in toReWrap)
        group.ReWrap();

      if (toReWrap.Count == 0) return;

      if (toReWrap.Any(CollectionViewGroup<T>.IsFullyExpanded))
        ScrollToTopItem();
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

    public void ExpandAll(CollectionViewGroup<T> group) {
      RootHolder.Clear();
      group.ExpandAll();
      RootHolder.Add(Root);
    }

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
        if (row.Items.Count > 0) {
          // TODO if item is selected => get next or previews not selected item in group or null
          TopItem = row.Items[0];
        }
      }

      return row != null || group != null;
    }

    public void ScrollToTopItem() {
      if (TopGroup?.Parent == null) return;

      var items = new List<object>();

      if (TopItem != null)
        foreach (var row in TopGroup.Items.OfType<CollectionViewRow<T>>()) {
          if (!row.Items.Contains(TopItem)) continue;
          items.Add(row);
          break;
        }

      items.Add(TopGroup);

      var group = TopGroup.Parent;
      while (group != null) {
        items.Add(group);
        group.IsExpanded = true;
        group = group.Parent;
      }
      items.Reverse();

      ScrollToItem = items;
    }
  }
}
