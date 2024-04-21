using System;

namespace MH.Utils.Extensions;

public static class ProgressExtensions {
  public static void Report(this IProgress<string> progress, string value, bool withTime) =>
    progress.Report(withTime ? $"{DateTime.Now:HH:mm:ss}: {value}" : value);
}