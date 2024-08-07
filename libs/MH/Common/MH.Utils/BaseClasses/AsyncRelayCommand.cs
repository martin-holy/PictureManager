using MH.Utils.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MH.Utils.BaseClasses;

public class AsyncRelayCommand : RelayCommandBase, IAsyncCommand {
  private NotifyTaskCompletion? _execution;

  public CancelAsyncCommand CancelCommand { get; }
  public NotifyTaskCompletion? Execution { get => _execution; protected set { _execution = value; OnPropertyChanged(); } }
  protected Func<CancellationToken, Task>? CommandFunc;

  protected AsyncRelayCommand(string? icon, string? text) : base(icon, text) {
    CancelCommand = new();
  }

  public AsyncRelayCommand(Func<CancellationToken, Task> command, string? icon = null, string? text = null) : this(icon, text) {
    CommandFunc = command;
  }

  public AsyncRelayCommand(Func<CancellationToken, Task> command, Func<bool> canExecute, string? icon = null, string? text = null) : this(icon, text) {
    CommandFunc = command;
    CanExecuteFunc = canExecute;
  }

  public override bool CanExecute(object? parameter) =>
    (Execution == null || Execution.IsCompleted) && base.CanExecute(parameter);

  public virtual async void Execute(object? parameter) {
    CancelCommand.NotifyCommandStarting();
    await ExecuteAsync(parameter, CommandFunc!(CancelCommand.Token));
  }

  public virtual async Task ExecuteAsync(object? parameter, Task task) {
    Execution = new(task, true);
    RaiseCanExecuteChanged();
    await Execution.TaskCompletion;
    CancelCommand.NotifyCommandFinished();
    RaiseCanExecuteChanged();
  }
}

public sealed class CancelAsyncCommand : RelayCommandBase, ICommand {
  private CancellationTokenSource _cts = new();
  private bool _executing;

  public CancellationToken Token => _cts.Token;

  public CancelAsyncCommand() {
    Text = "Cancel";
  }

  ~CancelAsyncCommand() {
    _cts.Dispose();
  }

  public void NotifyCommandStarting() {
    _executing = true;
    if (!_cts.IsCancellationRequested) return;
    _cts.Dispose();
    _cts = new();
    RaiseCanExecuteChanged();
  }

  public void NotifyCommandFinished() {
    _executing = false;
    RaiseCanExecuteChanged();
  }

  bool ICommand.CanExecute(object? parameter) =>
    _executing && !_cts.IsCancellationRequested;

  public void Execute(object? parameter) {
    _cts.Cancel();
    RaiseCanExecuteChanged();
  }
}

public class AsyncRelayCommand<T> : AsyncRelayCommand {
  protected Func<T?, CancellationToken, Task>? CommandParamFunc;
  protected Func<T?, bool>? CanExecuteParamFunc;

  public AsyncRelayCommand(Func<CancellationToken, Task> command, Func<T?, bool> canExecute, string? icon = null, string? text = null) : base(icon, text) {
    CommandFunc = command;
    CanExecuteParamFunc = canExecute;
  }

  public AsyncRelayCommand(Func<T?, CancellationToken, Task> command, string? icon = null, string? text = null) : base(icon, text) {
    CommandParamFunc = command;
  }

  public AsyncRelayCommand(Func<T?, CancellationToken, Task> command, Func<bool> canExecute, string? icon = null, string? text = null) : base(icon, text) {
    CommandParamFunc = command;
    CanExecuteFunc = canExecute;
  }

  public AsyncRelayCommand(Func<T?, CancellationToken, Task> command, Func<T?, bool> canExecute, string? icon = null, string? text = null) : base(icon, text) {
    CommandParamFunc = command;
    CanExecuteParamFunc = canExecute;
  }

  public override bool CanExecute(object? parameter) {
    if (Execution is { IsCompleted: false }) return false;
    if (CanExecuteFunc != null) return CanExecuteFunc();
    if (CanExecuteParamFunc != null) return CanExecuteParamFunc(Cast(parameter));

    return true;
  }

  public override async void Execute(object? parameter) {
    CancelCommand.NotifyCommandStarting();
    var task = (CommandFunc != null
      ? CommandFunc(CancelCommand.Token)
      : CommandParamFunc?.Invoke(Cast(parameter), CancelCommand.Token)) ?? Task.CompletedTask;
    await ExecuteAsync(parameter, task);
  }

  private static T? Cast(object? parameter) =>
    parameter is T cast ? cast : default;
}