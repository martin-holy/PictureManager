using MH.UI.Controls;
using MH.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Common.Dialogs;

public sealed class FileOperationDialogM : Dialog {
  private int _progressValue;
  private bool _isIndeterminate;
  private string _dirFrom = string.Empty;
  private string _dirTo = string.Empty;
  private string _fileName = string.Empty;

  public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }
  public bool IsIndeterminate { get => _isIndeterminate; set { _isIndeterminate = value; OnPropertyChanged(); } }
  public string DirFrom { get => _dirFrom; set { _dirFrom = value; OnPropertyChanged(); } }
  public string DirTo { get => _dirTo; set { _dirTo = value; OnPropertyChanged(); } }
  public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
  public CancellationTokenSource? WorkCts { get; private set; } = new();
  public object? WorkResult { get; private set; }
  public Task? WorkTask { get; set; }
  public IProgress<object[]> Progress { get; set; }

  public FileOperationDialogM(string title) : base(title, Res.IconImageMultiple) {
    ProgressValue = 0;

    Progress = new Progress<object[]>(e => {
      ProgressValue = (int)e[0];
      DirFrom = (string)e[1];
      DirTo = (string)e[2];
      FileName = (string)e[3];
    });

    Buttons = [new(CancelCommand, false, true)];
  }

  ~FileOperationDialogM() {
    if (WorkCts != null) {
      WorkCts.Dispose();
      WorkCts = null;
    }
  }

  public void SetWorkTask<T>(Task<T> work) {
    WorkTask = work.ContinueWith(res => {
      WorkResult = res.Result;
      return Tasks.RunOnUiThread(() => Result = 1);
    });
  }

  public override Task OnResultChanged(int result) {
    if (result != 0 || WorkCts == null) return Task.CompletedTask;
    WorkCts.Cancel();
    return WorkTask ?? Task.CompletedTask;
  }
}