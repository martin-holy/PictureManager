using MH.Utils.BaseClasses;
using System;
using System.Collections.ObjectModel;

namespace MH.Utils;

public static class Log {
  public static ObservableCollection<LogItem> Items { get; } = [];

  public static void Info(string msg, string detail) =>
    Add(new(LogLevel.Info, msg, detail));

  public static void Warning(string msg, string detail) =>
    Add(new(LogLevel.Warning, msg, detail));

  public static void Error(Exception ex) =>
    Error(ex, string.Empty);

  public static void Error(Exception ex, string msg) =>
    Error(string.IsNullOrEmpty(msg) ? ex.Message : msg,
      $"{msg}\n{ex.Message}\n{ex.StackTrace}");

  public static void Error(string msg, string detail) =>
    Add(new(LogLevel.Error, msg, detail));

  private static void Add(LogItem item) =>
    Tasks.RunOnUiThread(() => Items.Add(item));
}