using MH.UI.Controls;

namespace MH.UI.Dialogs;

public class MessageDialog : Dialog {
  private string _message;

  public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }

  public MessageDialog(string title, string message, string icon, bool canCancel, DialogButton[]? buttons = null) : base(title, icon) {
    _message = message;
    Buttons = buttons ?? (canCancel
      ? [new(YesCommand, true), new(NoCommand, false, true)]
      : [new(OkCommand, true)]);
  }
}