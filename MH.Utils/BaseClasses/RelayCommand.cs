using System;
using System.Windows.Input;

namespace MH.Utils.BaseClasses;

public static class RelayCommand {
  public static event EventHandler CanExecuteChangedEventHandler = delegate { };

  public static void InvokeCanExecuteChanged(object o, EventArgs e) =>
    CanExecuteChangedEventHandler(o, e);
}

public class RelayCommand<T> : ICommand {
  private readonly Action _command;
  private readonly Action<T> _commandWithParameter;
  private readonly Func<bool> _canExecute;
  private readonly Func<T, bool> _canExecuteWithParameter;

  public RelayCommand(Action command) {
    _command = command;
  }

  public RelayCommand(Action command, Func<bool> canExecute) {
    _command = command;
    _canExecute = canExecute;
  }

  public RelayCommand(Action command, Func<T, bool> canExecute) {
    _command = command;
    _canExecuteWithParameter = canExecute;
  }

  public RelayCommand(Action<T> command) {
    _commandWithParameter = command;
  }

  public RelayCommand(Action<T> command, Func<bool> canExecute) {
    _commandWithParameter = command;
    _canExecute = canExecute;
  }

  public RelayCommand(Action<T> command, Func<T, bool> canExecute) {
    _commandWithParameter = command;
    _canExecuteWithParameter = canExecute;
  }

  public bool CanExecute(object parameter) {
    if (_canExecute != null) return _canExecute();
    if (_canExecuteWithParameter != null) return _canExecuteWithParameter(Cast(parameter));

    return true;
  }

  public void Execute(object parameter) {
    _command?.Invoke();
    _commandWithParameter?.Invoke(Cast(parameter));
  }

  public event EventHandler CanExecuteChanged {
    add => RelayCommand.CanExecuteChangedEventHandler += value;
    remove => RelayCommand.CanExecuteChangedEventHandler -= value;
  }

  private static T Cast(object parameter) =>
    parameter is T cast ? cast : default;
}