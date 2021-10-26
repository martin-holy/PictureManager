using System;
using System.Windows.Input;

namespace PictureManager.Commands {
  public class RelayCommand : ICommand {
    private readonly Action _command;
    private readonly Action<object> _commandWithParameter;
    private readonly Func<bool> _canExecute;
    private readonly Func<object, bool> _canExecuteWithParameter;

    public RelayCommand(Action command) {
      _command = command;
    }

    public RelayCommand(Action command, Func<bool> canExecute) {
      _command = command;
      _canExecute = canExecute;
    }

    public RelayCommand(Action command, Func<object, bool> canExecute) {
      _command = command;
      _canExecuteWithParameter = canExecute;
    }

    public RelayCommand(Action<object> command) {
      _commandWithParameter = command;
    }

    public RelayCommand(Action<object> command, Func<bool> canExecute) {
      _commandWithParameter = command;
      _canExecute = canExecute;
    }

    public RelayCommand(Action<object> command, Func<object, bool> canExecute) {
      _commandWithParameter = command;
      _canExecuteWithParameter = canExecute;
    }

    public bool CanExecute(object parameter) {
      if (_canExecute != null) return _canExecute();
      if (_canExecuteWithParameter != null) return _canExecuteWithParameter(parameter);

      return true;
    }

    public void Execute(object parameter) {
      if (_command != null) _command();
      if (_commandWithParameter != null) _commandWithParameter(parameter);
    }

    public event EventHandler CanExecuteChanged;
  }
}
