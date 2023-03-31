using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace PictureManager.Domain.Dialogs {
  public sealed class FileOperationDialogM : ObservableObject, IDialog {
    private string _title;
    private int _result = -1;
    private int _progressValue;
    private bool _isIndeterminate;
    private string _dirFrom;
    private string _dirTo;
    private string _fileName;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }
    public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }
    public bool IsIndeterminate { get => _isIndeterminate; set { _isIndeterminate = value; OnPropertyChanged(); } }
    public string DirFrom { get => _dirFrom; set { _dirFrom = value; OnPropertyChanged(); } }
    public string DirTo { get => _dirTo; set { _dirTo = value; OnPropertyChanged(); } }
    public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }
    public CancellationTokenSource LoadCts { get; set; }
    public Task RunTask { get; set; }
    public IProgress<object[]> Progress { get; set; }
    public RelayCommand<object> CancelCommand { get; }

    public FileOperationDialogM(FileOperationMode mode, bool isIndeterminate) {
      Title = $"File Operation Dialog ({mode})";
      IsIndeterminate = isIndeterminate;
      ProgressValue = 0;

      Progress = new Progress<object[]>(e => {
        ProgressValue = (int)e[0];
        DirFrom = e[1].ToString();
        DirTo = e[2].ToString();
        FileName = e[3].ToString();
      });

      CancelCommand = new(Cancel);
    }

    ~FileOperationDialogM() {
      if (LoadCts != null) {
        LoadCts.Dispose();
        LoadCts = null;
      }
    }

    private async void Cancel() {
      if (LoadCts != null) {
        LoadCts.Cancel();
        await RunTask;
        Result = 0;
      }
    }
  }
}
