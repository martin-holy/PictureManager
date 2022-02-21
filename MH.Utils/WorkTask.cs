using System;
using System.Threading;
using System.Threading.Tasks;

namespace MH.Utils {
  public sealed class WorkTask : IDisposable {
    private CancellationTokenSource _cts;
    private Task _task;
    private bool _waitingForCancel;

    public CancellationToken Token { get; private set; }

    public async Task<bool> Cancel() {
      if (_task == null || _waitingForCancel) return false;

      _waitingForCancel = true;
      _cts?.Cancel();

      // TODO BUG unexpected error, task was canceled. problem when returning false from catch
      // to test it => resizing main window with thumbnails grid active will cause reloading to many times to fast
      try {
        if (_task.Status != TaskStatus.Canceled)
          await _task;
      }
      catch (Exception) {
        _waitingForCancel = false;
        return true;
      }

      _waitingForCancel = false;

      return true;
    }

    public Task Start(Task task) {
      _cts = new();
      Token = _cts.Token;
      _task = task;
      if (_task.Status == TaskStatus.Created)
        _task.Start();

      return _task.ContinueWith(_ => Dispose());
    }

    public void Dispose() {
      try {
        _task?.Dispose();
        _cts?.Dispose();
        _cts = null;
      }
      catch (Exception) {
        // ignored
      }
    }
  }
}
