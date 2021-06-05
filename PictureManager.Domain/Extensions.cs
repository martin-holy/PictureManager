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
    /// Tries to parse date and time from first 15 characters of the string 
    /// </summary>
    /// <param name="text">DateTime string in format yyyyMMdd_HHmmss</param>
    /// <param name="dateFormats">Example: {{"d", "d. "}, {"M", "MMMM "}, {"y", "yyyy"}}</param>
    /// <param name="timeFormat">Example: H:mm:ss</param>
    /// <returns>Formated "date, time" or string.Empty</returns>
    public static string DateTimeFromString(string text, Dictionary<string, string> dateFormats, string timeFormat) {
      if (string.IsNullOrEmpty(text) || text.Length < 15 || text[8] != '_') return string.Empty;

      var locDateFormats = dateFormats.ToDictionary(df => df.Key, df => df.Value);

      if (text.Substring(4, 2) == "00") {
        locDateFormats["M"] = string.Empty;
        text = $"{text.Substring(0, 5)}1{text.Substring(6, 9)}";
      }
      
      if (text.Substring(6, 2) == "00") {
        locDateFormats["d"] = string.Empty;
        text = $"{text.Substring(0, 7)}1{text.Substring(8, 7)}";
      }
      
      if (text.Length > 15) text = text.Substring(0, 15);

      if (!DateTime.TryParseExact(text, "yyyyMMdd_HHmmss", 
        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        return string.Empty;

      var dateFormat = locDateFormats.Aggregate(string.Empty, (f, current) => f + current.Value);
      var dateF = dt.ToString(dateFormat, CultureInfo.CurrentCulture);
      var timeF = dt.ToString(timeFormat, CultureInfo.CurrentCulture);

      return dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0 || string.IsNullOrEmpty(timeFormat) ? dateF : $"{dateF}, {timeF}";
    }

    public static string GetNewFileName(string directory, string fileName) {
      if (!Directory.Exists(directory)) return string.Empty;
      
      var name = Path.GetFileNameWithoutExtension(fileName);
      var ext = Path.GetExtension(fileName);
      var outFileName = fileName;
      var counter = 0;
      
      while (File.Exists(Path.Combine(directory, outFileName))) {
        counter++;
        outFileName = $"{name}{counter}{ext}";
      }

      return outFileName;
    }

    public static int FirstIndexOfLetter(this string s) {
      var index = 0;
      while (s.Length - 1 > index) {
        if (char.IsLetter(s, index))
          break;
        index++;
      }

      return index;
    }
  }
}
