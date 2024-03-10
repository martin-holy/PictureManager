using System;
using System.Collections.Generic;

namespace MH.Utils.Extensions {
  public static class ListExtensions {
    private static Random _random;
    public static void Shuffle<T>(this IList<T> list) {
      _random ??= new();

      var n = list.Count;
      while (n > 1) {
        n--;
        var k = _random.Next(n + 1);
        (list[k], list[n]) = (list[n], list[k]);
      }
    }

    public static void AddInOrder<T>(this List<T> list, T item, Func<T, T, int> compare) {
      int i;
      for (i = 0; i < list.Count; i++)
        if (compare.Invoke(list[i], item) > 0)
          break;

      list.Insert(i, item);
    }

    public static void Move<T>(this List<T> list, T item, int newIndex) {
      var oldIndex = list.IndexOf(item);
      if (newIndex == oldIndex) return;
      if (newIndex > oldIndex) newIndex--;

      list.RemoveAt(oldIndex);
      list.Insert(newIndex, item);
    }

    public static void Move<T>(this List<T> list, T item, T dest, bool aboveDest) {
      var oldIndex = list.IndexOf(item);
      var newIndex = list.IndexOf(dest);

      if (newIndex > oldIndex && aboveDest) newIndex--;
      if (newIndex < oldIndex && !aboveDest) newIndex++;
      if (newIndex == oldIndex) return;
      if (newIndex > oldIndex) newIndex--;

      list.RemoveAt(oldIndex);
      list.Insert(newIndex, item);
    }

    public static List<T> NullIfEmpty<T>(this List<T> list) =>
      list.Count == 0 ? null : list;

    public static List<T> Toggle<T>(this List<T> list, T item, bool nullIfEmpty) {
      if (list == null) {
        list = new() { item };
        return list;
      }

      if (!list.Remove(item))
        list.Add(item);

      if (nullIfEmpty && list.Count == 0)
        list = null;

      return list;
    }

    public static T GetNextOrPreviousItem<T>(this IList<T> items, IList<T> selected) {
      if (items == null) return default;
      if (selected == null) return default;
      if (selected.Count == 0) return default;

      var index = items.IndexOf(selected[^1]) + 1;
      if (index == items.Count)
        index = items.IndexOf(selected[0]) - 1;

      return index >= 0
        ? items[index]
        : default;
    }
  }
}
