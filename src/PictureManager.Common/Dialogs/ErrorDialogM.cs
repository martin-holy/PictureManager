using MH.UI.Controls;
using System;

namespace PictureManager.Common.Dialogs;

public sealed class ErrorDialogM : Dialog {
  public string Message { get; }
  public string Detail { get; }

  public ErrorDialogM(Exception ex, string title = "") : base(title, Res.IconBug) {
    Message = ex.Message;
    Detail = ex.InnerException == null
      ? ex.StackTrace ?? string.Empty
      : $"{ex.InnerException.Message}\n{ex.StackTrace}";
    Buttons = [new(CloseCommand, false, true)];

    if (string.IsNullOrEmpty(title))
      Title = "Error";
  }
}