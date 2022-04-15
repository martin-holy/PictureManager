using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;

namespace MH.Utils.Dialogs {
  public class InputDialog : ObservableObject, IDialog {
    private string _title;
    private string _message;
    private string _icon;
    private string _answer;
    private string _errorMessage;
    private bool _error;
    private int _result = -1;
    private readonly Func<string, string> _validator;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
    public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
    public string Answer { get => _answer; set { _answer = value; OnPropertyChanged(); } }
    public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); } }
    public bool Error { get => _error; set { _error = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnResultChanged(); } }

    public InputDialog(string title, string message, string icon, string answer, Func<string, string> validator) {
      Title = title;
      Message = message;
      Icon = icon;
      Answer = answer;
      _validator = validator;
    }

    private void OnResultChanged() {
      if (Result != 0) {
        OnPropertyChanged(nameof(Result));
        return;
      }

      ErrorMessage = _validator(Answer);
      if (string.IsNullOrEmpty(ErrorMessage))
        OnPropertyChanged(nameof(Result));
      else
        Error = true;
    }
  }
}
