using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;

namespace MH.UI.Interfaces {
  public interface ITreeView : INotifyPropertyChanged {
    public ExtObservableCollection<object> RootHolder { get; }
    public List<object> ScrollToItems { get; set; }
    public int ScrollToIndex { get; set; }
    public bool ScrollToTop { get; set; }
    public bool IsScrollUnitItem { get; set; }
    public bool IsSizeChanging { get; set; }
    public bool ShowTreeItemSelection { get; set; }
    public void OnTreeItemSelected(object value);
    public bool SetTopItem(object o);
    public void ScrollTo(ITreeItem o);
  }
}
