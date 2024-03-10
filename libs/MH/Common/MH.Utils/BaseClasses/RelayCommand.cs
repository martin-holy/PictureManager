using System;
using System.Windows.Input;

namespace MH.Utils.BaseClasses;

public class RelayCommand : ObservableObject, ICommand {
  protected Action CommandAction;
  protected Func<bool> CanExecuteFunc;

  public string Icon { get; set; }
  public string Text { get; set; }

  public static event EventHandler CanExecuteChangedEvent = delegate { };

  public event EventHandler CanExecuteChanged {
    add => CanExecuteChangedEvent += value;
    remove => CanExecuteChangedEvent -= value;
  }

  public RelayCommand() { }

  public RelayCommand(Action command, string icon = null, string text = null) : this(icon, text) {
    CommandAction = command;
  }

  public RelayCommand(Action command, Func<bool> canExecute, string icon = null, string text = null) : this(icon, text) {
    CommandAction = command;
    CanExecuteFunc = canExecute;
  }

  protected RelayCommand(string icon, string text) {
    Icon = icon;
    Text = text;
  }

  public static void InvokeCanExecuteChanged(object o, EventArgs e) =>
    CanExecuteChangedEvent(o, e);

  public virtual bool CanExecute(object parameter) =>
    CanExecuteFunc == null || CanExecuteFunc();

  public virtual void Execute(object parameter) =>
    CommandAction?.Invoke();
}

public class RelayCommand<T> : RelayCommand {
  protected Action<T> CommandParamAction;
  protected Func<T, bool> CanExecuteParamFunc;

  public RelayCommand(Action command, Func<T, bool> canExecute, string icon = null, string text = null) : base(icon, text) {
    CommandAction = command;
    CanExecuteParamFunc = canExecute;
  }

  public RelayCommand(Action<T> command, string icon = null, string text = null) : base(icon, text) {
    CommandParamAction = command;
  }

  public RelayCommand(Action<T> command, Func<bool> canExecute, string icon = null, string text = null) : base(icon, text) {
    CommandParamAction = command;
    CanExecuteFunc = canExecute;
  }

  public RelayCommand(Action<T> command, Func<T, bool> canExecute, string icon = null, string text = null) : base(icon, text) {
    CommandParamAction = command;
    CanExecuteParamFunc = canExecute;
  }

  public override bool CanExecute(object parameter) {
    if (CanExecuteFunc != null) return CanExecuteFunc();
    if (CanExecuteParamFunc != null) return CanExecuteParamFunc(Cast(parameter));

    return true;
  }

  public override void Execute(object parameter) {
    CommandAction?.Invoke();
    CommandParamAction?.Invoke(Cast(parameter));
  }

  private static T Cast(object parameter) =>
    parameter is T cast ? cast : default;
}