using System;
using MH.UI.WPF.Interfaces;

namespace MH.UI.WPF.EventsArgs {
  public class CatTreeViewItemDoppedEventArgs : EventArgs {
    public object Data { get; }
    public ICatTreeViewItem Dest { get; }
    public bool AboveDest { get; }
    public bool Copy { get; }

    public CatTreeViewItemDoppedEventArgs(object data, ICatTreeViewItem dest, bool aboveDest, bool copy) {
      Data = data;
      Dest = dest;
      AboveDest = aboveDest;
      Copy = copy;
    }
  }
}
