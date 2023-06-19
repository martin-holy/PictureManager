using MH.UI.Dialogs;
using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls {
  public class CollectionView<T> : ObservableObject, ICollectionView {
    private List<object> _scrollToItem;
    private bool _isSizeChanging;
    private T _topItem;
    private CollectionViewGroup<T> _topGroup;
    private readonly GroupByDialog<T> _groupByDialog = new();

    public object ObjectRoot => Root;
    public CollectionViewGroup<T> Root { get; set; }
    public T TopItem { get; set; }
    public CollectionViewGroup<T> TopGroup { get; set; }
    public List<object> ScrollToItem { get => _scrollToItem; set { _scrollToItem = value; OnPropertyChanged(); } }
    public bool IsSizeChanging { get => _isSizeChanging; set => OnSizeChanging(value); }

    public RelayCommand<CollectionViewGroup<T>> OpenGroupByDialogCommand { get; }

    public CollectionView(string icon, string title) {
      Root = new(null, new(null, title), null, icon, null) { View = this };

      OpenGroupByDialogCommand = new(OpenGroupByDialog);
    }

    public virtual int GetItemWidth(object item) => throw new NotImplementedException();
    public virtual IEnumerable<CollectionViewGroupByItem<T>> GetGroupByItems(IEnumerable<T> source) => throw new NotImplementedException();
    public virtual string ItemOrderBy(T item) => throw new NotImplementedException();
    public virtual void Select(IEnumerable<T> source, T item, bool isCtrlOn, bool isShiftOn) => throw new NotImplementedException();

    public void Select(object row, object item, bool isCtrlOn, bool isShiftOn) {
      if (row is not CollectionViewRow<T> r || item is not T i) return;
      Select(r.Group.Source, i, isCtrlOn, isShiftOn);
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

    private void OpenGroupByDialog(CollectionViewGroup<T> group) =>
      _groupByDialog.Open(group, GetGroupByItems(group.Source));

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
