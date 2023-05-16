using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MH.Utils {
  public class Selecting<T> where T : ISelectable {
    public ObservableCollection<T> Items { get; } = new();

    public event EventHandler<ObjectEventArgs<T>> ItemChangedEventHandler = delegate { };
    public event EventHandler<ObjectEventArgs<T[]>> ItemsChangedEventHandler = delegate { };
    public event EventHandler AllDeselectedEventHandler = delegate { };

    public bool SetSelected(T item, bool value) {
      if (item.IsSelected == value) return false;

      item.IsSelected = value;

      if (value)
        Items.Add(item);
      else
        Items.Remove(item);

      ItemChangedEventHandler(this, new(item));
      return true;
    }

    public void DeselectAll() {
      if (Items.Count == 0) return;

      foreach (var item in Items)
        item.IsSelected = false;

      Items.Clear();
      AllDeselectedEventHandler(this, EventArgs.Empty);
    }

    public void Select(IEnumerable<T> items) {
      var change = false;

      foreach (var item in Items.Except(items).ToArray()) {
        item.IsSelected = false;
        Items.Remove(item);
        change = true;
      }

      foreach (var item in items.Except(Items).ToArray()) {
        item.IsSelected = true;
        Items.Add(item);
        change = true;
      }

      if (change)
        ItemsChangedEventHandler(this, new(Items.ToArray()));
    }

    public void Select(List<T> items, T item, bool isCtrlOn, bool isShiftOn) {
      // single select
      if (!isCtrlOn && !isShiftOn) {
        DeselectAll();

        if (SetSelected(item, true))
          ItemsChangedEventHandler(this, new(Items.ToArray()));

        return;
      }

      // single invert select
      if (isCtrlOn) {
        if (SetSelected(item, !item.IsSelected))
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
        if (SetSelected(items[i], true))
          change = true;

      if (change)
        ItemsChangedEventHandler(this, new(Items.ToArray()));
    }
  }
}
