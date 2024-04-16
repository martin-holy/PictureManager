using System;
using System.Threading.Tasks;

namespace MH.Utils;

public static class Tasks {
  public static TaskScheduler UiTaskScheduler { get; private set; }

  public static void SetUiTaskScheduler() =>
    UiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

  public static Task RunOnUiThread(Action action) {
    var task = new Task(action);
    task.Start(UiTaskScheduler);
    return task;
  }

  public static Task<T> RunOnUiThread<T>(Func<T> func) {
    var task = new Task<T>(func);
    task.Start(UiTaskScheduler);
    return task;
  }

  public static Action<Action> Dispatch { get; set; }

  /// <summary>
  /// Executes the work on background thread and than executes the onSuccess or the onError on UI thread
  /// </summary>
  public static void DoWork<T>(Func<T> work, Action<T> onSuccess, Action<Exception> onError) {
    Task.Run(work).ContinueWith(task => {
      if (task.IsFaulted)
        onError(task.Exception?.InnerException);
      else
        onSuccess(task.Result);
    }, UiTaskScheduler);
  }
}