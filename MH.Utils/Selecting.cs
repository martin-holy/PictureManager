using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MH.Utils {
  public class Selecting<T> where T : ISelectable {
    public ObservableCollection<T> Items { get; } = new();

    public event EventHandler<ObjectEventArgs<T[]>> ItemsChangedEventHandler = delegate { };
    public event EventHandler AllDeselectedEventHandler = delegate { };

    public bool Set(T item, bool value) {
      if (item.IsSelected == value) return false;

      item.IsSelected = value;

      if (value && !Items.Contains(item)) {
        Items.Add(item);
        return true;
      }

      if (!value && Items.Contains(item)) {
        Items.Remove(item);
        return true;
      }

      return false;
    }

    public bool Set(IEnumerable<T> items, bool value) {
      var change = false;

      foreach (var item in items)
        if (Set(item, value))
          change = true;

      return change;
    }

    public void Set(IEnumerable<T> items) {
      if (Set(Items.Except(items), false) || Set(items.Except(Items), true))
        ItemsChangedEventHandler(this, new(Items.ToArray()));
    }

    public void Add(IEnumerable<T> items) {
      if (Set(items.Except(Items), true))
        ItemsChangedEventHandler(this, new(Items.ToArray()));
    }

    public void DeselectAll() {
      if (Items.Count == 0) return;

      foreach (var item in Items)
        item.IsSelected = false;

      Items.Clear();
      AllDeselectedEventHandler(this, EventArgs.Empty);
    }

    public void Select(List<T> items, T item, bool isCtrlOn, bool isShiftOn) {
      // single select
      if (!isCtrlOn && !isShiftOn) {
        DeselectAll();

        if (Set(item, true))
          ItemsChangedEventHandler(this, new(Items.ToArray()));

        return;
      }

      // single invert select
      if (isCtrlOn) {
        if (Set(item, !item.IsSelected))
          ItemsChangedEventHandler(this, new(Items.ToArray()));

        return;
      }

      if (items == null) return;

      // multi select
      var indexOfItem = items.IndexOf(item);
      var fromItem = items.FirstOrDefault(x => x.IsSelected && !Equals(x, item));
      var from = fromItem == null ? 0 : items.IndexOf(fromItem);
      var to = indexOfItem;
      var change = false;

      if (from > to) {
        to = from;
        from = indexOfItem;
      }

      for (var i = from; i < to + 1; i++)
        if (Set(items[i], true))
          change = true;

      if (change)
        ItemsChangedEventHandler(this, new(Items.ToArray()));
    }
  }
}
