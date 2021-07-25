using System.Threading;
using System.Threading.Tasks;

namespace PictureManager.Domain.Utils {
  public class WorkTask {
    private CancellationTokenSource _cts;
    private Task _task;

    public CancellationToken Token { get; private set; }

    public async Task Cancel() {
      if (_task != null) {
        _cts?.Cancel();
        await _task;
      }
    }

    public Task Start(Task task) {
      _cts = new CancellationTokenSource();
      Token = _cts.Token;
      _task = task;
      if (_task.Status == TaskStatus.Created)
        _task.Start();

      return _task.ContinueWith((x) => { Dispose(); });
    }

    private void Dispose() {
      _task?.Dispose();
      _cts?.Dispose();
      _cts = null;
    }
  }
}
