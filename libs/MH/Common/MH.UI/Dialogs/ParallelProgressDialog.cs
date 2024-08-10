using System;
using System.Threading;
using System.Threading.Tasks;

namespace MH.UI.Dialogs;

public class ParallelProgressDialog<T>(string title, string icon, T[] items, string? actionIcon, string? actionText)
  : ProgressDialog<T>(title, icon, items, actionIcon, actionText) {
  public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

  protected override Task Do(T[] items, CancellationToken token) {
    var po = new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = token };
    Parallel.ForEach(items, po, item => Do(item));
    return Task.CompletedTask;
  }
}