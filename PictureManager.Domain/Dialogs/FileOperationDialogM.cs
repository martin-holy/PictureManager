using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Domain.Dialogs;

public sealed class FileOperationDialogM : Dialog {
  private int _progressValue;
  private bool _isIndeterminate;
  private string _dirFrom;
  private string _dirTo;
  private string _fileName;

  public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }
  public bool IsIndeterminate { get => _isIndeterminate; set { _isIndeterminate = value; OnPropertyChanged(); } }
  public string DirFrom { get => _dirFrom; set { _dirFrom = value; OnPropertyChanged(); } }
  public string DirTo { get => _dirTo; set { _dirTo = value; OnPropertyChanged(); } }
  public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
  public CancellationTokenSource LoadCts { get; set; }
  public Task RunTask { get; set; }
  public IProgress<object[]> Progress { get; set; }

  public FileOperationDialogM(FileOperationMode mode, bool isIndeterminate) : base("", Res.IconImageMultiple) {
    Title = $"File Operation Dialog ({mode})";
    IsIndeterminate = isIndeterminate;
    ProgressValue = 0;

    Progress = new Progress<object[]>(e => {
      ProgressValue = (int)e[0];
      DirFrom = e[1].ToString();
      DirTo = e[2].ToString();
      FileName = e[3].ToString();
    });

    Buttons = new DialogButton[] { new(CancelCommand, false, true) };
  }

  ~FileOperationDialogM() {
    if (LoadCts != null) {
      LoadCts.Dispose();
      LoadCts = null;
    }
  }

  public override Task OnResultChanged(int result) {
    if (result != 0 || LoadCts == null) return Task.CompletedTask;
    LoadCts.Cancel();
    return RunTask;
  }
}