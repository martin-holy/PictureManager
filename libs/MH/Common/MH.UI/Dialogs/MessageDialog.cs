using MH.Utils.BaseClasses;

namespace MH.Utils.Dialogs;

public class MessageDialog : Dialog {
  private string _message;

  public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }

  public MessageDialog(string title, string message, string icon, bool canCancel, DialogButton[] buttons = null) : base(title, icon) {
    Message = message;
    Buttons = buttons ?? (canCancel
      ? new DialogButton[] { new(YesCommand, true), new(NoCommand, false, true) }
      : new DialogButton[] { new(OkCommand, true) });
  }
}