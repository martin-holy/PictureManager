﻿using MH.Utils;
using MH.Utils.BaseClasses;
using System;
using System.Threading.Tasks;

namespace MH.UI.Controls;

public class Dialog : ObservableObject {
  private string _title;
  private string _icon;
  private int _result = -1;
  private DialogButton[] _buttons = [];

  public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
  public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
  public DialogButton[] Buttons { get => _buttons; set { _buttons = value; OnPropertyChanged(); } }
  public static Func<Dialog, int> Show { get; set; } = _ => -1;

  public int Result {
    get => _result;
    set {
      _result = value;
      OnResultChanged(value).ContinueWith(_ => Tasks.RunOnUiThread(() => OnPropertyChanged()));
    }
  }

  public static RelayCommand<Dialog> CancelCommand { get; } = new(x => SetResult(x, 0), null, "Cancel");
  public static RelayCommand<Dialog> CloseCommand { get; } = new(x => SetResult(x, 0), null, "Close");
  public static RelayCommand<Dialog> NoCommand { get; } = new(x => SetResult(x, 0), null, "No");
  public static RelayCommand<Dialog> OkCommand { get; } = new(x => SetResult(x, 1), null, "Ok");
  public static RelayCommand<Dialog> YesCommand { get; } = new(x => SetResult(x, 1), null, "Yes");

  public Dialog(string title, string icon) {
    _title = title;
    _icon = icon;
  }

  public static void SetResult(Dialog? dialog, int result) {
    if (dialog != null) dialog.Result = result;
  }

  public RelayCommand SetResult(int result, string? icon, string? text) =>
    new(() => Result = result, icon, text);

  protected virtual Task OnResultChanged(int result) => Task.CompletedTask;
}