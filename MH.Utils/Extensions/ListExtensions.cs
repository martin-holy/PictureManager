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

    public static void AddInOrder<T>(this List<T> list, T item, Func<T, T, bool> compare) {
      int i;
      for (i = 0; i < list.Count; i++)
        if (compare.Invoke(list[i], item))
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

    public static List<T> Toggle<T>(List<T> list, T item, bool nullIfEmpty) where T : new() {
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
  }
}
