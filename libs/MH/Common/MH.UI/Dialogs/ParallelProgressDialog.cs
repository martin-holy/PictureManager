using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Dialogs;

public class ParallelProgressDialog<T>(string title, string icon, T[] items, string? actionIcon, string? actionText, bool autoClose = true)
  : ProgressDialog<T>(title, icon, items, actionIcon, actionText, autoClose) {

  protected override Task Do(T[] items, CancellationToken token) =>
    Parallel.ForEachAsync(items, token, (item, _) => new(Do(item, token)));
}