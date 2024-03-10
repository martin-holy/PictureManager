using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MH.Utils;

public class TaskQueue<T> {
  public bool IsRunning { get; private set; }
  public readonly HashSet<T> Items = new();

  public void Add(T item) =>
    Items.Add(item);

  public async void Start(Action<T> workAction, Action<T> doneAction) {
    if (IsRunning) return;
    if (Items.Count == 0) {
      IsRunning = false;
      return;
    }

    IsRunning = true;
    var items = Items.ToArray();

    await Task.WhenAll(
      from partition in Partitioner.Create(items).GetPartitions(Environment.ProcessorCount)
      select Task.Run(() => {
        using (partition)
          while (partition.MoveNext())
            workAction(partition.Current);
        return Task.CompletedTask;
      }));

    foreach (var item in items) {
      Items.Remove(item);
      doneAction(item);
    }

    IsRunning = false;
    Start(workAction, doneAction);
  }
}