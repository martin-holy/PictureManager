using System.ComponentModel;
using System.Windows;

namespace PictureManager.Dialogs {
  /// <summary>
  /// Interaction logic for ProgressBarDialog.xaml
  /// </summary>
  public partial class ProgressBarDialog {
    public BackgroundWorker Worker;

    public ProgressBarDialog() {
      InitializeComponent();
      Worker = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
      Worker.ProgressChanged += worker_ProgressChanged;
      Worker.RunWorkerCompleted += worker_RunWorkerCompleted;
    }

    private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
      PbWorkerProgress.Value = e.ProgressPercentage;
      LblProgress.Content = e.UserState;
    }

    private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
      Close();
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      Worker.CancelAsync();
      Close();
    }
  }
}
