using System;
using System.Threading.Tasks;

namespace MH.Utils.BaseClasses;

public class NotifyTaskCompletion : ObservableObject {
  public Task Task { get; }
  public Task TaskCompletion { get; }
  public TaskStatus Status => Task.Status;
  public bool IsCompleted => Task.IsCompleted;
  public bool IsNotCompleted => !Task.IsCompleted;
  public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;
  public bool IsCanceled => Task.IsCanceled;
  public bool IsFaulted => Task.IsFaulted;
  public AggregateException? Exception => Task.Exception;
  public Exception? InnerException => Exception?.InnerException;
  public string? ErrorMessage => InnerException?.Message;

  public NotifyTaskCompletion(Task task) {
    Task = task;
    TaskCompletion = task.IsCompleted ? Task.CompletedTask : WatchTaskAsync(task);
  }

  private async Task WatchTaskAsync(Task task) {
    try {
      await task;
    }
    catch {
      // ignored
    }

    NotifyPropertyChanged(task);
  }

  protected virtual void NotifyPropertyChanged(Task task) {
    OnPropertyChanged(nameof(Status));
    OnPropertyChanged(nameof(IsCompleted));
    OnPropertyChanged(nameof(IsNotCompleted));
    if (task.IsCanceled) {
      OnPropertyChanged(nameof(IsCanceled));
    }
    else if (task.IsFaulted) {
      OnPropertyChanged(nameof(IsFaulted));
      OnPropertyChanged(nameof(Exception));
      OnPropertyChanged(nameof(InnerException));
      OnPropertyChanged(nameof(ErrorMessage));
    }
    else {
      OnPropertyChanged(nameof(IsSuccessfullyCompleted));
    }
  }
}

public sealed class NotifyTaskCompletion<TResult>(Task<TResult> task) : NotifyTaskCompletion(task) {
  public TResult? Result => Task.Status == TaskStatus.RanToCompletion ? ((Task<TResult>)Task).Result : default;

  protected override void NotifyPropertyChanged(Task task) {
    base.NotifyPropertyChanged(task);
    if (!task.IsCanceled && !task.IsFaulted)
      OnPropertyChanged(nameof(Result));
  }
}