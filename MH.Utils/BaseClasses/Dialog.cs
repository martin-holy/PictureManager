using MH.Utils.Dialogs;
using System;
using System.Threading.Tasks;

namespace MH.Utils.BaseClasses;

public class Dialog : ObservableObject {
  private string _title;
  private string _icon;
  private int _result = -1;
  private DialogButton[] _buttons;

  public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
  public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
  public DialogButton[] Buttons { get => _buttons; set { _buttons = value; OnPropertyChanged(); } }
  public static Func<Dialog, int> Show { get; set; }

  public int Result {
    get => _result;
    set {
      _result = value;
      OnResultChanged(value).ContinueWith(_ => Tasks.RunOnUiThread(() => OnPropertyChanged()));
    }
  }

  public static RelayCommand<Dialog> CancelCommand { get; } = new(x => x.Result = 0, Res.IconXCross, "Cancel");
  public static RelayCommand<Dialog> CloseCommand { get; } = new(x => x.Result = 0, Res.IconXCross, "Close");
  public static RelayCommand<Dialog> NoCommand { get; } = new(x => x.Result = 0, Res.IconXCross, "No");
  public static RelayCommand<Dialog> OkCommand { get; } = new(x => x.Result = 1, Res.IconCheckMark, "Ok");
  public static RelayCommand<Dialog> YesCommand { get; } = new(x => x.Result = 1, Res.IconCheckMark, "Yes");

  public Dialog(string title, string icon) {
    Title = title;
    Icon = icon;
  }

  public RelayCommand SetResult(int result, string icon, string text) =>
    new(() => Result = result, icon, text);

  public virtual Task OnResultChanged(int result) => Task.CompletedTask;
}