using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

    public static int FirstIndexOfLetter(this string s) {
      var index = 0;
      while (s.Length - 1 > index) {
        if (char.IsLetter(s, index))
          break;
        index++;
      }

      return index;
    }

    public static void DeleteDirectoryIfEmpty(string path) {
      if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).GetEnumerator().MoveNext())
        Directory.Delete(path);
    }

    /// <summary>
    /// Move directory but don't crash when directory exists or not exists
    /// </summary>
    /// <param name="srcPath"></param>
    /// <param name="destPath"></param>
    public static void MoveDirectory(string srcPath, string destPath) {
      if (Directory.Exists(destPath)) {
        var srcPathLength = srcPath.TrimEnd(Path.DirectorySeparatorChar).Length + 1;

        foreach (var dir in Directory.EnumerateDirectories(srcPath)) {
          MoveDirectory(dir, Path.Combine(destPath, dir.Substring(srcPathLength)));
        }

        foreach (var file in Directory.EnumerateFiles(srcPath)) {
          var destFilePath = Path.Combine(destPath, file.Substring(srcPathLength));
          if (File.Exists(destFilePath))
            File.Delete(destFilePath);
          File.Move(file, destFilePath);
        }

        if (!Directory.EnumerateFileSystemEntries(srcPath).GetEnumerator().MoveNext()) {
          Directory.Delete(srcPath);
        }
      }
      else {
        var destParentPath = Path.GetDirectoryName(destPath.TrimEnd(Path.DirectorySeparatorChar)) ??
                             throw new ArgumentNullException();
        Directory.CreateDirectory(destParentPath);
        Directory.Move(srcPath, destPath);
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

    private static Random _random;

    public static void Shuffle<T>(this IList<T> list) {
      if (_random == null)
        _random = new Random();

      var n = list.Count;
      while (n > 1) {
        n--;
        var k = _random.Next(n + 1);
        var value = list[k];
        list[k] = list[n];
        list[n] = value;
      }
    }

    public static string FileSizeToString(long size) {
      string[] sizes = { "B", "KB", "MB", "GB" };
      var order = 0;
      var doubleSize = (double) size;
      while (doubleSize >= 1024 && order + 1 < sizes.Length) {
        order++;
        doubleSize /= 1024;
      }

      return $"{doubleSize:0.##} {sizes[order]}";
    }
  }
}
