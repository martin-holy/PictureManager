using System;

namespace SimpleDB {
  public interface ILogger {
    void LogError(Exception ex);
    void LogError(Exception ex, string msg);
  }
}