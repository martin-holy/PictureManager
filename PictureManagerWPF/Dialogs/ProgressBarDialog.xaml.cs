using System.ComponentModel;
using System.Windows;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for ProgressBarDialog.xaml
  /// </summary>
  public partial class ProgressBarDialog {
    public BackgroundWorker worker;

    public ProgressBarDialog() {
      InitializeComponent();
      worker = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
      worker.ProgressChanged += worker_ProgressChanged;
      worker.RunWorkerCompleted += worker_RunWorkerCompleted;
    }

    private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
      PbWorkerProgress.Value = e.ProgressPercentage;
    }

    private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
      Close();
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      worker.CancelAsync();
      Close();
    }
  }
}
