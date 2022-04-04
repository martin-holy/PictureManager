using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace MH.Utils.Dialogs {
  public delegate int MessageDialogShow(string title, string message, string icon, bool canCancel, string[] buttons = null);

  public class MessageDialog : ObservableObject, IDialog {
    private string _title;
    private string _message;
    private string _icon;
    private bool _canCancel;
    private string[] _buttons;
    private int _result = -1;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
    public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
    public bool CanCancel { get => _canCancel; set { _canCancel = value; OnPropertyChanged(); } }
    public string[] Buttons { get => _buttons; set { _buttons = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }

    public MessageDialog(string title, string message, string icon, bool canCancel, string[] buttons = null) {
      Title = title;
      Message = message;
      Icon = icon;
      CanCancel = canCancel;
      Buttons = buttons
        ?? (canCancel
          ? new[] { "YES", "NO" }
          : new[] { "OK" });
    }
  }
}
