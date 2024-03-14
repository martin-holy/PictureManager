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
}