using System.IO;

namespace MH.Utils.Extensions;

public static class IOExtensions {
  /// <summary>
  /// Combine two paths with no checks!
  /// </summary>
  /// <param name="path1">path with no directory separator on the end</param>
  /// <param name="path2"></param>
  /// <returns></returns>
  public static string PathCombine(string path1, string path2) =>
    string.Join(null, path1, Path.DirectorySeparatorChar, path2);

  public static void DeleteDirectoryIfEmpty(string path) {
    using var enumerator = Directory.EnumerateFileSystemEntries(path).GetEnumerator();
    if (Directory.Exists(path) && !enumerator.MoveNext())
      Directory.Delete(path);
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
}