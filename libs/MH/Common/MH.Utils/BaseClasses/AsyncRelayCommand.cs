using MH.Utils.Interfaces;
using System;
using System.Threading.Tasks;

namespace MH.Utils.BaseClasses;

public class AsyncRelayCommand : RelayCommandBase, IAsyncCommand {
  protected Func<Task>? CommandFunc;
  
  protected AsyncRelayCommand() { }

  protected AsyncRelayCommand(string? icon, string? text) : base(icon, text) { }

  public AsyncRelayCommand(Func<Task> command, string? icon = null, string? text = null) : this(icon, text) {
    CommandFunc = command;
  }

  public AsyncRelayCommand(Func<Task> command, Func<bool> canExecute, string? icon = null, string? text = null) : this(icon, text) {
    CommandFunc = command;
    CanExecuteFunc = canExecute;
  }

  public virtual async void Execute(object? parameter) {
    await ExecuteAsync(parameter);
  }

  public virtual Task ExecuteAsync(object? parameter) =>
    CommandFunc?.Invoke() ?? Task.CompletedTask;
}

public class AsyncRelayCommand<T> : AsyncRelayCommand {
  protected Func<T?, Task>? CommandParamFunc;
  protected Func<T?, bool>? CanExecuteParamFunc;

  public AsyncRelayCommand(Func<Task> command, Func<T?, bool> canExecute, string? icon = null, string? text = null) : base(icon, text) {
    CommandFunc = command;
    CanExecuteParamFunc = canExecute;
  }

  public AsyncRelayCommand(Func<T?, Task> command, string? icon = null, string? text = null) : base(icon, text) {
    CommandParamFunc = command;
  }

  public AsyncRelayCommand(Func<T?, Task> command, Func<bool> canExecute, string? icon = null, string? text = null) : base(icon, text) {
    CommandParamFunc = command;
    CanExecuteFunc = canExecute;
  }

  public AsyncRelayCommand(Func<T?, Task> command, Func<T?, bool> canExecute, string? icon = null, string? text = null) : base(icon, text) {
    CommandParamFunc = command;
    CanExecuteParamFunc = canExecute;
  }

  public override bool CanExecute(object? parameter) {
    if (CanExecuteFunc != null) return CanExecuteFunc();
    if (CanExecuteParamFunc != null) return CanExecuteParamFunc(Cast(parameter));

    return true;
  }

  public override Task ExecuteAsync(object? parameter) =>
    (CommandFunc != null
      ? CommandFunc()
      : CommandParamFunc?.Invoke(Cast(parameter))) ?? Task.CompletedTask;

  private static T? Cast(object? parameter) =>
    parameter is T cast ? cast : default;
}