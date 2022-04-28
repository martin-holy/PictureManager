using System;
using System.IO;
using System.Linq;

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

    public static bool TryParseDoubleUniversal(this string s, out double result) {
      result = 0.0;
      if (string.IsNullOrEmpty(s)) return false;

      var clean = new string(s.Where(x => char.IsDigit(x) || x == '.' || x == ',' || x == '-').ToArray());
      var iOfSep = clean.LastIndexOfAny(new[] { ',', '.' });
      var partA = clean.Substring(0, iOfSep).Replace(",", string.Empty).Replace(".", string.Empty);
      var partB = clean.Substring(iOfSep + 1);
      if (!int.TryParse(partA, out var intA)) return false;
      if (!int.TryParse(partB, out var intB)) return false;
      if (intA < 0) intB *= -1;
      var dp = double.Parse("1".PadRight(partB.Length + 1, '0'));

      result = intA + intB / dp;
      return true;
    }
  }
}
