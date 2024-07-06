using System;

namespace MH.Utils.BaseClasses;

public abstract class RelayCommandBase {
  public string? Icon { get; set; }
  public string? Text { get; set; }

  protected Func<bool>? CanExecuteFunc;

  public static event EventHandler CanExecuteChangedEvent = delegate { };

  public event EventHandler? CanExecuteChanged {
    add => CanExecuteChangedEvent += value;
    remove => CanExecuteChangedEvent -= value;
  }

  protected RelayCommandBase() { }

  protected RelayCommandBase(string? icon, string? text) {
    Icon = icon;
    Text = text;
  }

  public static void InvokeCanExecuteChanged(object o, EventArgs e) =>
    CanExecuteChangedEvent(o, e);

  public virtual bool CanExecute(object? parameter) =>
    CanExecuteFunc == null || CanExecuteFunc();
}