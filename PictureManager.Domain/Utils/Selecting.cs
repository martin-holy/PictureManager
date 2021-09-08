using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Utils {
  public interface ISelectable {
    bool IsSelected { get; set; }
  }

  public static class Selecting {
    public static void SetSelected<T>(ref List<T> selected, ISelectable item, bool value, Action onChange) {
      if (item.IsSelected == value) return;
      item.IsSelected = value;
      if (value) selected.Add((T)item);
      else selected.Remove((T)item);
      onChange?.Invoke();
    }

    public static void DeselectAll<T>(ref List<T> selected, Action onChange) {
      foreach (var item in selected.Cast<ISelectable>().ToArray())
        SetSelected(ref selected, item, false, onChange);
    }

    public static void Select<T>(ref List<T> selected, List<T> items, ISelectable item, bool isCtrlOn, bool isShiftOn, Action onChange) {
      // single select
      if (!isCtrlOn && !isShiftOn) {
        DeselectAll<T>(ref selected, onChange);
        SetSelected(ref selected, item, true, onChange);
        return;
      }

      // single invert select
      if (isCtrlOn) {
        SetSelected(ref selected, item, !item.IsSelected, onChange);
        return;
      }

      // multi select
      if (isShiftOn && items != null) {
        var indexOfItem = items.IndexOf((T)item);
        var fromItem = items.Cast<ISelectable>().FirstOrDefault(x => x.IsSelected && x != item);
        var from = fromItem == null ? 0 : items.IndexOf((T)fromItem);
        var to = indexOfItem;

        if (from > to) {
          to = from;
          from = indexOfItem;
        }

        for (var i = from; i < to + 1; i++)
          SetSelected(ref selected, (ISelectable)items[i], true, onChange);
      }
    }
  }
}
