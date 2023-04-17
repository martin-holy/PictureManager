using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using System;

namespace PictureManager.Domain.Dialogs {
  public sealed class ErrorDialogM : Dialog {
    private string _message;
    private string _detail;

    public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
    public string Detail { get => _detail; set { _detail = value; OnPropertyChanged(); } }

    public ErrorDialogM(Exception ex, string title = "") : base(title, Res.IconBug) {
      Message = ex.Message;
      Detail = ex.InnerException == null
        ? ex.StackTrace
        : $"{ex.InnerException.Message}\n{ex.StackTrace}";
      Buttons = new DialogButton[] { new("Close", Res.IconXCross, CloseCommand, false, true) };

      if (string.IsNullOrEmpty(title))
        Title = "Error";
    }
  }
}
