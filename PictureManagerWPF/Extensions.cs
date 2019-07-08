using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager {
  static class Extensions {
    public static void Sort<TSource, TKey>(this ObservableCollection<TSource> collection,
      Func<TSource, TKey> keySelector) {
      var sorted = collection.OrderBy(keySelector).ToList();
      for (var i = 0; i < sorted.Count; i++) {
        collection.Move(collection.IndexOf(sorted[i]), i);
      }
    }

    public static int IntParseOrDefault(this string s, int d) {
      return int.TryParse(s, out var result) ? result : d;
    }
  }
}
