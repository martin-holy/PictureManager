namespace MH.Utils.BaseClasses;

public enum LogLevel { Info, Warning, Error }

public class LogItem(LogLevel level, string title, string detail) {
  public LogLevel Level { get; } = level;
  public string Title { get; } = title;
  public string Detail { get; } = detail;
}