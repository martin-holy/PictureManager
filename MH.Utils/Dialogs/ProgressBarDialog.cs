using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace MH.Utils.Dialogs {
  public delegate int ProgressBarDialogShow(ProgressBarDialog dialog);

  public class ProgressBarDialog : ObservableObject, IDialog {
    private readonly BackgroundWorker _worker;
    private readonly CancellationTokenSource _cts;
    private readonly ParallelOptions _po;

    private string _title;
    private string _message;
    private bool _canCancel;
    private int _result = -1;
    private string _stringProgress;
    private int _intProgress;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string Message { get => _message; set { _message = value; OnPropertyChanged(); } }
    public bool CanCancel { get => _canCancel; set { _canCancel = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }
    public string StringProgress { get => _stringProgress; set { _stringProgress = value; OnPropertyChanged(); } }
    public int IntProgress { get => _intProgress; set { _intProgress = value; OnPropertyChanged(); } }

    public ProgressBarDialog(string title, bool canCancel, int maxDegreeOfParallelism) {
      Title = title;
      CanCancel = canCancel;

      _worker = new() {
        WorkerReportsProgress = true,
        WorkerSupportsCancellation = canCancel
      };

      _cts = new();

      _po = new() {
        MaxDegreeOfParallelism = maxDegreeOfParallelism,
        CancellationToken = _cts.Token
      };

      PropertyChanged += (_, e) => {
        if (nameof(Result).Equals(e.PropertyName, StringComparison.Ordinal) && Result == -1)
          _worker.CancelAsync();
      };
    }

    ~ProgressBarDialog() {
      _cts.Dispose();
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
        Result = 0;
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
