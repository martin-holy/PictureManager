using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MH.Utils;

public class TaskQueue<T>(int queueSize, Action<T> workAction, Action<T> doneAction) {
  private bool _isRunning;
  private readonly HashSet<T> _items = [];

  public void Add(T item) =>
    _items.Add(item);

  public async void Start() {
    if (_isRunning) return;
    if (_items.Count == 0) {
      _isRunning = false;
      return;
    }

    _isRunning = true;
    var items = _items.Take(queueSize).ToArray();

    await Task.WhenAll(
      from partition in Partitioner.Create(items).GetPartitions(Environment.ProcessorCount)
      select Task.Run(() => {
        using (partition)
          while (partition.MoveNext())
            workAction(partition.Current);
        return Task.CompletedTask;
      }));

    foreach (var item in items) {
      _items.Remove(item);
      doneAction(item);
    }

    _isRunning = false;
    Start();
  }
}