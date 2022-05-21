using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace MH.Utils.Dialogs {
  public class MessageDialog : ObservableObject, IDialog {
    private string _title;
    private string _message;
    private string _icon;
    private bool _canCancel;
    private DialogButton[] _buttons;
    private int _result = -1;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
    public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
    public bool CanCancel { get => _canCancel; set { _canCancel = value; OnPropertyChanged(); } }
    public DialogButton[] Buttons { get => _buttons; set { _buttons = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }

    public MessageDialog(string title, string message, string icon, bool canCancel, DialogButton[] buttons = null) {
      Title = title;
      Message = message;
      Icon = icon;
      CanCancel = canCancel;
      Buttons = buttons
        ?? (canCancel
          ? new DialogButton[] { new("YES", 0, "IconCheckMark", true), new("NO", 1, "IconXCross", false, true) }
          : new DialogButton[] { new("OK", 0, "IconCheckMark", true) });
    }
  }
}
