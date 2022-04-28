using MH.Utils.Interfaces;
using System;

namespace MH.Utils.EventsArgs {
  public class TreeItemDoppedEventArgs : EventArgs {
    public object Data { get; }
    public ITreeItem Dest { get; }
    public bool AboveDest { get; }
    public bool Copy { get; }

    public TreeItemDoppedEventArgs(object data, ITreeItem dest, bool aboveDest, bool copy) {
      Data = data;
      Dest = dest;
      AboveDest = aboveDest;
      Copy = copy;
    }
  }
}
