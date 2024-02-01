using System.Windows.Input;

namespace MH.Utils.Dialogs {
  public class DialogButton {
    public string Text { get; }
    public string Icon { get; }
    public bool IsDefault { get; }
    public bool IsCancel { get; }
    public ICommand Command { get; }

    public DialogButton(string text, string icon, ICommand command, bool isDefault = false, bool isCancel = false) {
      Text = text;
      Command = command;
      Icon = icon;
      IsDefault = isDefault;
      IsCancel = isCancel;
    }
  }
}
