using MH.Utils.Extensions;
using System.Collections.Generic;
using System.ComponentModel;
using MH.Utils.Interfaces;

namespace MH.UI.Interfaces {
  public interface ITreeView : INotifyPropertyChanged {
    public ExtObservableCollection<object> RootHolder { get; }
    public List<object> ScrollToItems { get; set; }
    public int ScrollToIndex { get; set; }
    public bool ScrollToTop { get; set; }
    public bool IsScrollUnitItem { get; set; }
    public bool IsSizeChanging { get; set; }
    public bool SetTopItem(object o);
    public void ScrollTo(ITreeItem o);
  }
}
