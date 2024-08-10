﻿using MH.UI.Controls;
using MH.Utils.BaseClasses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Dialogs;

public class ProgressDialog<T> : Dialog {
  private readonly T[] _items;
  private int _progressMax;
  private int _progressValue;
  private int _progressIndex;
  private string? _progressText;
  private readonly IProgress<(int, string)> _progress;

  public int ProgressMax { get => _progressMax; set { _progressMax = value; OnPropertyChanged(); } }
  public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }
  public string? ProgressText { get => _progressText; set { _progressText = value; OnPropertyChanged(); } }
  public AsyncRelayCommand ActionCommand { get; }

  public ProgressDialog(string title, string icon, T[] items, string? actionIcon, string? actionText) : base(title, icon) {
    ActionCommand = new(DoAction, CanAction, actionIcon, actionText);
    _items = items;
    _progressMax = _items.Length;
    _progress = new Progress<(int, string)>(x => {
      ProgressValue = x.Item1;
      ProgressText = x.Item2;
    });

    Buttons = actionIcon == null && actionText == null
      ? [new(CloseCommand, true, true)]
      : [new(ActionCommand, true), new(CloseCommand, false, true)];
  }

  protected void AutoRun() {
    if (ActionCommand.CanExecute(null))
      ActionCommand.Execute(null);
  }

  protected void ReportProgress(string msg) =>
    _progress.Report((_progressIndex++, msg));

  protected override Task OnResultChanged(int result) {
    if (result == 0) ActionCommand.CancelCommand.Execute(null);
    return Task.CompletedTask;
  }

  protected virtual bool CanAction() => true;

  protected virtual bool DoBefore() => true;

  protected virtual Task Do(T item) => Task.CompletedTask;

  protected virtual async Task Do(T[] items, CancellationToken token) {
    foreach (var item in _items) {
      if (token.IsCancellationRequested) break;
      await Do(item).ConfigureAwait(false);
    }
  }

  protected virtual void DoAfter() { }

  private async Task DoAction(CancellationToken token) {
    try {
      if (!DoBefore()) return;

      await Task.Run(async () => {
        _progressIndex = 0;
        await Do(_items, token).ConfigureAwait(false);
      }, token);
    }
    finally {
      DoAfter();
      Result = 1;
    }
  }
}