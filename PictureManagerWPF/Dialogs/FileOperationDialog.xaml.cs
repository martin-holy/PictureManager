using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public BackgroundWorker Worker;
    public string DirFrom { get => _dirFrom; set { _dirFrom = value; OnPropertyChanged(); } }
    public string DirTo { get => _dirTo; set { _dirTo = value; OnPropertyChanged(); } }
    public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(); } }

    public FileOperationDialog() {
      InitializeComponent();
      Worker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
      Worker.ProgressChanged += Worker_ProgressChanged;
      Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
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

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
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
