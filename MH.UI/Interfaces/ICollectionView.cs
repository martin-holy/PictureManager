using MH.Utils.Extensions;
using System.Collections.Generic;
using System.ComponentModel;

namespace MH.UI.Interfaces {
  public interface ICollectionView : INotifyPropertyChanged {
    public ExtObservableCollection<object> RootHolder { get; }
    public List<object> ScrollToItems { get; set; }
    public bool ScrollToTop { get; set; }
    public bool IsSizeChanging { get; set; }
    public void OpenItem(object item);
    public void SelectItem(object row, object item, bool isCtrlOn, bool isShiftOn);
    public void SetExpanded(object group);
    public bool SetTopItem(object o);
  }
}
