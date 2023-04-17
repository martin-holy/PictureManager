using MH.Utils.BaseClasses;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace MH.Utils.Dialogs {
  public class ProgressBarDialog : Dialog {
    private readonly BackgroundWorker _worker;
    private readonly CancellationTokenSource _cts;
    private readonly ParallelOptions _po;
    private readonly bool _canCancel;

    private string _message;
    private string _stringProgress;
    private int _intProgress;

    public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
    public string StringProgress { get => _stringProgress; set { _stringProgress = value; OnPropertyChanged(); } }
    public int IntProgress { get => _intProgress; set { _intProgress = value; OnPropertyChanged(); } }

    public ProgressBarDialog(string title, string icon, bool canCancel, int maxDegreeOfParallelism) : base(title, icon) {
      _canCancel = canCancel;
      CloseCommand = new(Cancel, () => _canCancel);
      Buttons = new DialogButton[] { new("Cancel", "IconXCross", CloseCommand, false, true) };

      _worker = new() {
        WorkerReportsProgress = true,
        WorkerSupportsCancellation = canCancel
      };

      _cts = new();

      _po = new() {
        MaxDegreeOfParallelism = maxDegreeOfParallelism,
        CancellationToken = _cts.Token
      };
    }

    ~ProgressBarDialog() {
      _cts.Dispose();
    }

    private void Cancel() {
      if (_canCancel)
        _worker.CancelAsync();

      Result = 0;
    }

    public void Start() =>
      _worker.RunWorkerAsync();

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
        Result = 1;
      };

      _worker.ProgressChanged += (_, e) => {
        if (e.UserState is object[] userState) {
          Message = (string)userState[0];
          StringProgress = $"{userState[1]} / {userState[2]}";
        }

        IntProgress = e.ProgressPercentage;
      };
    }
  }
}
