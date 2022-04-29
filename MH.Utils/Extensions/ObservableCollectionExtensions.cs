using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MH.Utils.Extensions {
  public static class ObservableCollectionExtensions {
    public static bool Sort<TSource, TKey>(this ObservableCollection<TSource> collection, Func<TSource, TKey> keySelector) {
      var sorted = collection.OrderBy(keySelector).ToList();
      var modified = false;
      for (var newI = 0; newI < sorted.Count; newI++) {
        var oldI = collection.IndexOf(sorted[newI]);
        if (newI != oldI) {
          collection.Move(oldI, newI);
          modified = true;
        }
      }
      return modified;
    }

    public static int SetRelativeTo<T>(this ObservableCollection<T> collection, T item, T dest, bool aboveDest) {
      var oldIdx = collection.IndexOf(item);
      var newIdx = collection.IndexOf(dest);

      if (aboveDest && oldIdx > -1 && oldIdx < newIdx) newIdx--;
      if (!aboveDest && (oldIdx < 0 || oldIdx > newIdx)) newIdx++;

      if (oldIdx < 0)
        collection.Insert(newIdx, item);
      else
        collection.Move(oldIdx, newIdx);

      return newIdx;
    }

    public static int SetInOrder<T>(this ObservableCollection<T> collection, T item, Func<T, string> keySelector) {
      int newIdx;
      var strB = keySelector(item);
      for (newIdx = 0; newIdx < collection.Count; newIdx++) {
        var strA = keySelector(collection[newIdx]);
        var cRes = string.Compare(strA, strB, StringComparison.CurrentCultureIgnoreCase);
        if (collection[newIdx].Equals(item) || cRes < 0) continue;

        break;
      }

      var oldIdx = collection.IndexOf(item);
      if (oldIdx < 0)
        collection.Insert(newIdx, item);
      else if (oldIdx != newIdx) {
        if (newIdx > oldIdx) newIdx--;
        collection.Move(oldIdx, newIdx);
      }

      return newIdx;
    }

    public static bool Toggle<T>(this ObservableCollection<T> collection, T item) {
      if (collection.Remove(item))
        return false;

      collection.Add(item);
      return true;
    }

    public static ObservableCollection<T> Toggle<T>(ObservableCollection<T> collection, T item, bool nullIfEmpty) where T : new() {
      if (collection == null) {
        collection = new() { item };
        return collection;
      }

      if (!collection.Remove(item))
        collection.Add(item);

      if (nullIfEmpty && collection.Count == 0)
        collection = null;

      return collection;
    }
  }
}
