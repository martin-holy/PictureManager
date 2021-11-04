using System;
using System.Collections.Generic;
using System.Linq;
using MH.Utils.Interfaces;

namespace MH.Utils {
  public static class Selecting {
    public static void SetSelected<T>(List<T> selected, ISelectable item, bool value, Action onChange) {
      if (item.IsSelected == value) return;
      item.IsSelected = value;
      if (value) selected.Add((T)item);
      else selected.Remove((T)item);
      onChange?.Invoke();
    }

    public static void DeselectAll<T>(List<T> selected, Action onChange) {
      foreach (var item in selected.Cast<ISelectable>().ToArray())
        SetSelected(selected, item, false, onChange);
    }

    public static void Select<T>(List<T> selected, List<T> items, ISelectable item, bool isCtrlOn, bool isShiftOn, Action onChange) {
      // single select
      if (!isCtrlOn && !isShiftOn) {
        DeselectAll(selected, onChange);
        SetSelected(selected, item, true, onChange);
        return;
      }

      // single invert select
      if (isCtrlOn) {
        SetSelected(selected, item, !item.IsSelected, onChange);
        return;
      }

      if (items == null) return;

      // multi select
      var indexOfItem = items.IndexOf((T)item);
      var fromItem = items.Cast<ISelectable>().FirstOrDefault(x => x.IsSelected && !Equals(x, item));
      var from = fromItem == null ? 0 : items.IndexOf((T)fromItem);
      var to = indexOfItem;

      if (from > to) {
        to = from;
        from = indexOfItem;
      }

      for (var i = from; i < to + 1; i++)
        SetSelected(selected, (ISelectable)items[i], true, onChange);
    }
  }
}
