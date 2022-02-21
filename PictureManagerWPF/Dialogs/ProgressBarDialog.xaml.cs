using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PictureManager.Dialogs {
  public partial class ProgressBarDialog {
    private readonly BackgroundWorker _worker;
    private readonly CancellationTokenSource _cts;
    private readonly ParallelOptions _po;

    public ProgressBarDialog(bool canCancel, int maxDegreeOfParallelism, string title) {
      InitializeComponent();

      Owner = Application.Current.MainWindow;
      Title = title;
      _worker = new() {
        WorkerReportsProgress = true,
        WorkerSupportsCancellation = canCancel
      };
      _cts = new();
      _po = new() {
        MaxDegreeOfParallelism = maxDegreeOfParallelism,
        CancellationToken = _cts.Token
      };

      if (!canCancel)
        BtnCancel.Visibility = Visibility.Collapsed;
    }

    ~ProgressBarDialog() {
      _cts.Dispose();
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      _worker.CancelAsync();
      Close();
    }

    public void AddEvents<T>(T[] items, Func<bool> doBeforeLoop, Action<T> action, Func<T, string> customMessage, Action<object, RunWorkerCompletedEventArgs> onCompleted) {
      _worker.DoWork += delegate (object o, DoWorkEventArgs e) {
        if (doBeforeLoop != null && !doBeforeLoop()) return;

        var worker = (BackgroundWorker)o;
        var count = items.Length;
        var done = 0;

        try {
          Parallel.ForEach(items, _po, item => {
            if (worker.CancellationPending) {
              e.Cancel = true;
              _cts.Cancel();
            }

            done++;
            worker.ReportProgress(Convert.ToInt32((double)done / count * 100),
              new object[] { customMessage(item), done, count });

            action(item);
          });
        }
        catch (OperationCanceledException) { }
      };

      _worker.RunWorkerCompleted += (o, e) => {
        onCompleted?.Invoke(o, e);
        Close();
      };

      _worker.ProgressChanged += (o, e) => {
        var userState = (object[])e.UserState;
        TbCustomMessage.Text = (string)userState[0];
        TbCount.Text = $"{userState[1]} / {userState[2]}";

        PbWorkerProgress.Value = e.ProgressPercentage;
      };
    }

    public void Start() {
      _worker.RunWorkerAsync();
      Show();
    }

    public void StartDialog() {
      _worker.RunWorkerAsync();
      ShowDialog();
    }
  }
}
