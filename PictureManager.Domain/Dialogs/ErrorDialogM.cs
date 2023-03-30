using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;

namespace PictureManager.Domain.Dialogs {
  public sealed class ErrorDialogM : ObservableObject, IDialog {
    private string _title;
    private string _message;
    private string _detail;
    private int _result = -1;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
    public string Detail { get => _detail; set { _detail = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }

    public RelayCommand<object> CloseCommand { get; }

    public ErrorDialogM(Exception ex, string title = "") {
      CloseCommand = new(() =>  Result = 0);
      Title = string.IsNullOrEmpty(title) ? "Error" : title;
      Message = ex.Message;
      Detail = ex.InnerException == null
        ? ex.StackTrace
        : $"{ex.InnerException.Message}\n{ex.StackTrace}";
    }
  }
}
