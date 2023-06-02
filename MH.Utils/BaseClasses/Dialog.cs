using MH.Utils.Dialogs;
using System;

namespace MH.Utils.BaseClasses {
  public class Dialog : ObservableObject {
    private string _title;
    private string _icon;
    private int _result = -1;
    private DialogButton[] _buttons;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }
    public DialogButton[] Buttons { get => _buttons; set { _buttons = value; OnPropertyChanged(); } }
    public static Func<Dialog, int> Show { get; set; }

    public RelayCommand<object> CloseCommand { get; set; }
    public RelayCommand<object> YesOkCommand { get; set; }

    public Dialog(string title, string icon) {
      Title = title;
      Icon = icon;
      CloseCommand = new(() => Result = 0);
      YesOkCommand = new(() => Result = 1);
    }

    public RelayCommand<object> SetResult(int result) =>
      new(() => Result = result);
  }
}
