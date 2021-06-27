using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace PictureManager {
  public static class Extensions {
    /// <summary>
    /// Move directory but don't crash when directory exists or not exists
    /// </summary>
    /// <param name="srcPath"></param>
    /// <param name="destPath"></param>
    public static void MoveDirectory(string srcPath, string destPath) {
      if (Directory.Exists(destPath)) {
        var srcPathLength = srcPath.TrimEnd(Path.DirectorySeparatorChar).Length + 1;

        foreach (var dir in Directory.EnumerateDirectories(srcPath)) {
          MoveDirectory(dir, Path.Combine(destPath, dir[srcPathLength..]));
        }

        foreach (var file in Directory.EnumerateFiles(srcPath)) {
          var destFilePath = Path.Combine(destPath, file[srcPathLength..]);
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
                             throw new ArgumentNullException(nameof(destPath));
        Directory.CreateDirectory(destParentPath);
        Directory.Move(srcPath, destPath);
      }
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
      var doubleSize = (double)size;
      while (doubleSize >= 1024 && order + 1 < sizes.Length) {
        order++;
        doubleSize /= 1024;
      }

      return $"{doubleSize:0.##} {sizes[order]}";
    }

    public static T FindTemplatedParent<T>(FrameworkElement child) where T : FrameworkElement {
      while (true) {
        if (child?.TemplatedParent == null) return null;
        if (child.TemplatedParent is T parent) return parent;
        child = (FrameworkElement)child.TemplatedParent;
      }
    }

    public static T FindThisOrParent<T>(FrameworkElement child, string name) where T : FrameworkElement {
      while (true) {
        if (child == null) return null;
        if (child is T element && element.Name.Equals(name, StringComparison.Ordinal)) return element;
        child = (FrameworkElement)(child.Parent ?? child.TemplatedParent);
      }
    }

    public static bool TryParseDoubleUniversal(this string s, out double result) {
      // TODO refactor

      result = 0.0;
      if (string.IsNullOrEmpty(s)) return false;

      var clean = new string(s.Where(x => char.IsDigit(x) || x == '.' || x == ',').ToArray());
      var iOfSep = clean.LastIndexOfAny(new[] { ',', '.' });
      var partA = clean.Substring(0, iOfSep).Replace(",", string.Empty).Replace(".", string.Empty);
      var partB = clean.Substring(iOfSep + 1);
      if (!int.TryParse(partA, out var intA)) return false;
      if (!int.TryParse(partB, out var intB)) return false;

      var bla = double.Parse("1".PadRight(partB.Length + 1, '0'));

      result = intA + intB / bla;

      return true;
    }
  }
}
