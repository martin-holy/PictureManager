using MH.Utils.Interfaces;
using System;

namespace MH.Utils.EventsArgs {
  public class TreeItemDroppedEventArgs : EventArgs {
    public object Data { get; }
    public ITreeItem Dest { get; }
    public bool AboveDest { get; }
    public bool Copy { get; }

    public TreeItemDroppedEventArgs(object data, ITreeItem dest, bool aboveDest, bool copy) {
      Data = data;
      Dest = dest;
      AboveDest = aboveDest;
      Copy = copy;
    }
  }
}
