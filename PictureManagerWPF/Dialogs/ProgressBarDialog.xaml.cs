using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PictureManager.Dialogs {
  public partial class ProgressBarDialog {
    public BackgroundWorker Worker;
    public CancellationTokenSource Cts;
    public ParallelOptions Po;

    public ProgressBarDialog(Window owner, bool canCancel, int maxDegreeOfParallelism, string title) {
      InitializeComponent();

      Owner = owner;
      Title = title;
      Worker = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = canCancel};
      Cts = new CancellationTokenSource();
      Po = new ParallelOptions {MaxDegreeOfParallelism = maxDegreeOfParallelism, CancellationToken = Cts.Token};

      if (!canCancel)
        BtnCancel.Visibility = Visibility.Collapsed;
    }

    ~ProgressBarDialog() {
      Cts.Dispose();
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e) {
      Worker.CancelAsync();
      Close();
    }

    public void AddEvents<T>(T[] items, Func<bool> doBeforeLoop, Action<T> action, Func<T, string> customMessage, Action<object, RunWorkerCompletedEventArgs> onCompleted) {
      Worker.DoWork += delegate(object o, DoWorkEventArgs e) {
        if (doBeforeLoop != null && !doBeforeLoop()) return;

        var worker = (BackgroundWorker) o;
        var count = items.Length;
        var done = 0;

        try {
          Parallel.ForEach(items, Po, item => {
            if (worker.CancellationPending) {
              e.Cancel = true;
              Cts.Cancel();
            }

            done++;
            worker.ReportProgress(Convert.ToInt32(((double) done / count) * 100),
              new object[] {customMessage(item), done, count});

            action(item);
          });
        }
        catch (OperationCanceledException) { }
      };

      Worker.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs e) {
        onCompleted?.Invoke(sender, e);
        Close();
      };

      Worker.ProgressChanged += delegate(object sender, ProgressChangedEventArgs e) {
        var userState = (object[]) e.UserState;
        TbCustomMessage.Text = (string) userState[0];
        TbCount.Text = $"{userState[1]} / {userState[2]}";

        PbWorkerProgress.Value = e.ProgressPercentage;
      };
    }

    public void Start() {
      Worker.RunWorkerAsync();
      Show();
    }

    public void StartDialog() {
      Worker.RunWorkerAsync();
      ShowDialog();
    }
  }
}
