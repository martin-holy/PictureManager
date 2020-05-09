using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PictureManager.Domain {
  public static class Extensions {
    public static int IntParseOrDefault(this string s, int d) {
      return Int32.TryParse(s, out var result) ? result : d;
    }

    public static void Sort<TSource, TKey>(this ObservableCollection<TSource> collection,
      Func<TSource, TKey> keySelector) {
      var sorted = collection.OrderBy(keySelector).ToList();
      for (var i = 0; i < sorted.Count; i++) {
        collection.Move(collection.IndexOf(sorted[i]), i);
      }
    }

    /// <summary>
    /// Combine two paths with no checks!
    /// </summary>
    /// <param name="path1">path with no directory separator on the end</param>
    /// <param name="path2"></param>
    /// <returns></returns>
    public static string PathCombine(string path1, string path2) {
      return path1 + Path.DirectorySeparatorChar + path2;
    }

    public static void DeleteDirectoryIfEmpty(string path) {
      if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).GetEnumerator().MoveNext())
        Directory.Delete(path);
    }
  }
}
