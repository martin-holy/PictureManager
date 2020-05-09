using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for FileOperationDialog.xaml
  /// </summary>
  public partial class FileOperationDialog : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private string _dirFrom;
    private string _dirTo;
    private string _fileName;

    public CancellationTokenSource LoadCts { get; set; }
    public Task RunTask { get; set; }
    public IProgress<string[]> Progress;
    public BackgroundWorker Worker;
    public string DirFrom { get => _dirFrom; set { _dirFrom = value; OnPropertyChanged(); } }
    public string DirTo { get => _dirTo; set { _dirTo = value; OnPropertyChanged(); } }
    public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }

    public FileOperationDialog() {
      InitializeComponent();

      Progress = new Progress<string[]>(e => {
        DirFrom = e[0];
        DirTo = e[1];
        FileName = e[2];
      });

      Worker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
      Worker.ProgressChanged += Worker_ProgressChanged;
      Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
    }

    ~FileOperationDialog() {
      if (LoadCts != null) {
        LoadCts.Dispose();
        LoadCts = null;
      }
    }

    private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
      Close();
    }

    private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
      PbProgress.Value = e.ProgressPercentage;
      DirFrom = ((string[]) e.UserState)[0];
      DirTo = ((string[]) e.UserState)[1];
      FileName = ((string[]) e.UserState)[2];
    }

    private async void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      if (LoadCts != null) {
        LoadCts.Cancel();
        await RunTask;
        Close();
      }

      Worker.CancelAsync();
      Close();
    }

    private void FileOperationDialog_OnClosing(object sender, CancelEventArgs e) {
      if (Worker == null) return;
      Worker.Dispose();
      Worker = null;
    }
  }
}
