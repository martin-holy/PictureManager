using MH.Utils.Interfaces;
using System;
using System.Threading.Tasks;

namespace MH.Utils.BaseClasses;

public class AsyncRelayCommand : ObservableObject, IAsyncCommand {
  protected Func<Task> CommandFunc;
  protected Func<bool> CanExecuteFunc;

  public string Icon { get; set; }
  public string Text { get; set; }

  public static event EventHandler CanExecuteChangedEvent = delegate { };

  public event EventHandler CanExecuteChanged {
    add => CanExecuteChangedEvent += value;
    remove => CanExecuteChangedEvent -= value;
  }

  public AsyncRelayCommand() { }

  public AsyncRelayCommand(Func<Task> command, string icon = null, string text = null) : this(icon, text) {
    CommandFunc = command;
  }

  public AsyncRelayCommand(Func<Task> command, Func<bool> canExecute, string icon = null, string text = null) : this(icon, text) {
    CommandFunc = command;
    CanExecuteFunc = canExecute;
  }

  protected AsyncRelayCommand(string icon, string text) {
    Icon = icon;
    Text = text;
  }

  public static void InvokeCanExecuteChanged(object o, EventArgs e) =>
    CanExecuteChangedEvent(o, e);

  public virtual bool CanExecute(object parameter) =>
    CanExecuteFunc == null || CanExecuteFunc();

  public virtual async void Execute(object parameter) {
    await ExecuteAsync(parameter);
  }

  public virtual Task ExecuteAsync(object parameter) =>
    CommandFunc?.Invoke();
}

public class AsyncRelayCommand<T> : AsyncRelayCommand {
  protected Func<T, Task> CommandParamFunc;
  protected Func<T, bool> CanExecuteParamFunc;

  public AsyncRelayCommand(Func<Task> command, Func<T, bool> canExecute, string icon = null, string text = null) : base(icon, text) {
    CommandFunc = command;
    CanExecuteParamFunc = canExecute;
  }

  public AsyncRelayCommand(Func<T, Task> command, string icon = null, string text = null) : base(icon, text) {
    CommandParamFunc = command;
  }

  public AsyncRelayCommand(Func<T, Task> command, Func<bool> canExecute, string icon = null, string text = null) : base(icon, text) {
    CommandParamFunc = command;
    CanExecuteFunc = canExecute;
  }

  public AsyncRelayCommand(Func<T, Task> command, Func<T, bool> canExecute, string icon = null, string text = null) : base(icon, text) {
    CommandParamFunc = command;
    CanExecuteParamFunc = canExecute;
  }

  public override bool CanExecute(object parameter) {
    if (CanExecuteFunc != null) return CanExecuteFunc();
    if (CanExecuteParamFunc != null) return CanExecuteParamFunc(Cast(parameter));

    return true;
  }

  public override Task ExecuteAsync(object parameter) =>
    CommandFunc != null
      ? CommandFunc()
      : CommandParamFunc?.Invoke(Cast(parameter));

  private static T Cast(object parameter) =>
    parameter is T cast ? cast : default;
}