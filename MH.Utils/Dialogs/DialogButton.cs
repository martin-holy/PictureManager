using MH.Utils.BaseClasses;

namespace MH.Utils.Dialogs {
  public class DialogButton {
    public string Text { get; }
    public string Icon { get; }
    public bool IsDefault { get; }
    public bool IsCancel { get; }
    public RelayCommand<object> Command { get; }

    public DialogButton(string text, string icon, RelayCommand<object> command, bool isDefault = false, bool isCancel = false) {
      Text = text;
      Command = command;
      Icon = icon;
      IsDefault = isDefault;
      IsCancel = isCancel;
    }
  }
}
