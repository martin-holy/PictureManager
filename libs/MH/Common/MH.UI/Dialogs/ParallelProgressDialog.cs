using MH.UI.Controls;
using MH.Utils.BaseClasses;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Dialogs;

public class ParallelProgressDialog<T> : Dialog {
  private readonly T[] _items;
  private int _progressMax;
  private int _progressValue;
  private int _progressIndex;
  private string? _progressText;
  private readonly IProgress<(int, string)> _progress;

  public int ProgressMax { get => _progressMax; set { _progressMax = value; OnPropertyChanged(); } }
  public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }
  public string? ProgressText { get => _progressText; set { _progressText = value; OnPropertyChanged(); } }
  public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
  public AsyncRelayCommand ActionCommand { get; }

  public ParallelProgressDialog(string title, string icon, T[] items, string? actionIcon, string? actionText) : base(title, icon) {
    ActionCommand = new(DoAction, CanAction, actionIcon, actionText);
    _items = items;
    _progressMax = _items.Length;
    _progress = new Progress<(int, string)>(x => {
      ProgressValue = x.Item1;
      ProgressText = x.Item2;
    });

    Buttons = [
      new(ActionCommand, true),
      new(CloseCommand, false, true)
    ];
  }

  public void ReportProgress(string msg) =>
    _progress.Report((_progressIndex++, msg));

  protected override Task OnResultChanged(int result) {
    if (result == 0) ActionCommand.CancelCommand.Execute(null);
    return Task.CompletedTask;
  }

  protected virtual bool CanAction() => true;

  protected virtual bool DoBefore() => true;

  protected virtual Task Do(T item) => Task.CompletedTask;

  protected virtual void DoAfter() { }

  private async Task DoAction(CancellationToken token) {
    try {
      if (!DoBefore()) return;

      await Task.Run(() => {
        _progressIndex = 0;
        var po = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = token };
        Parallel.ForEach(_items, po, item => Do(item));
      }, token);
    }
    finally {
      DoAfter();
      Result = 1;
    }
  }
}