﻿using System;
using System.Collections.Generic;
using System.IO;

namespace PictureManager {
  static class Extensions {
    public static int FirstIndexOfLetter(this string s) {
      var index = 0;
      while (s.Length - 1 > index) {
        if (char.IsLetter(s, index))
          break;
        index++;
      }

      return index;
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
