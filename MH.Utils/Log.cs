using MH.Utils.BaseClasses;
using System;
using System.Collections.ObjectModel;

namespace MH.Utils {
  public static class Log {
    public static ObservableCollection<LogItem> Items { get; } = new();

    public static void Error(Exception ex) =>
      Error(ex, string.Empty);

    public static void Error(Exception ex, string msg) =>
      Tasks.RunOnUiThread(() =>
        Items.Add(new(
          string.IsNullOrEmpty(msg)
            ? ex.Message
            : msg,
          $"{msg}\n{ex.Message}\n{ex.StackTrace}")));
  }
}
