using System;
using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Domain.Utils {
  public sealed class WorkTask : IDisposable {
    private CancellationTokenSource _cts;
    private Task _task;
    private bool _waitingForCancel;

    public CancellationToken Token { get; private set; }

    public async Task<bool> Cancel() {
      if (_task == null || _waitingForCancel) return false;

      _waitingForCancel = true;
      _cts?.Cancel();
      if (_task.Status != TaskStatus.Canceled)
        await _task;
      _waitingForCancel = false;

      return true;
    }

    public Task Start(Task task) {
      _cts = new CancellationTokenSource();
      Token = _cts.Token;
      _task = task;
      if (_task.Status == TaskStatus.Created)
        _task.Start();

      return _task.ContinueWith((_) => Dispose());
    }

    public void Dispose() {
      try {
        _task?.Dispose();
        _cts?.Dispose();
        _cts = null;
      }
      catch (Exception) { }
    }
  }
}
