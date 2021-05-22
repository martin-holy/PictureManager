using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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

    public static void AddInOrder<T>(this List<T> list, T item, Func<T, T, bool> compare) {
      int i;
      for (i = 0; i < list.Count; i++)
        if (compare.Invoke(list[i], item))
          break;

      list.Insert(i, item);
    }

    public static void Move<T>(this List<T> list, T item, T dest, bool aboveDest) {
      var oldIndex = list.IndexOf(item);
      var newIndex = list.IndexOf(dest);

      if (newIndex > oldIndex && aboveDest) newIndex--;
      if (newIndex < oldIndex && !aboveDest) newIndex++;
      if (newIndex == oldIndex) return;

      list.RemoveAt(oldIndex);
      list.Insert(newIndex, item);
    }

    public static void Move<T>(this ObservableCollection<T> collection, T item, T dest, bool aboveDest) {
      var oldIndex = collection.IndexOf(item);
      var newIndex = collection.IndexOf(dest);

      if (newIndex > oldIndex && aboveDest) newIndex--;
      if (newIndex < oldIndex && !aboveDest) newIndex++;
      if (newIndex == oldIndex) return;

      collection.Move(oldIndex, newIndex);
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

    /// <summary>
    /// Tries to parse date from first 8 characters of the string 
    /// </summary>
    /// <param name="s">String date in format yyyyMMdd</param>
    /// <param name="formats">Example: {{"d", "d. "}, {"m", "MMMM "}, {"y", "yyyy"}}</param>
    /// <returns>Formated date or string.Empty</returns>
    public static string DateFromString(string s, Dictionary<string, string> formats) {
      if (s.Length < 8) return string.Empty;

      var dateString = s.Substring(0, 8);

      if (!dateString.All(char.IsDigit)) return string.Empty;

      try {
        var y = int.Parse(dateString.Substring(0, 4));
        var m = int.Parse(dateString.Substring(4, 2));
        var d = int.Parse(dateString.Substring(6, 2));

        if (m == 0) {
          formats["m"] = string.Empty;
          m++;
        }

        if (d == 0) {
          formats["d"] = string.Empty;
          d++;
        }

        var date = new DateTime(y, m, d);
        var format = formats.Aggregate(string.Empty, (f, current) => f + current.Value);

        return date.ToString(format, CultureInfo.CurrentCulture);
      }
      catch (Exception) {
        return string.Empty;
      }
    }
  }
}
