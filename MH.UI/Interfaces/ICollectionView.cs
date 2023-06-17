using System.Collections.Generic;
using System.ComponentModel;

namespace MH.UI.Interfaces {
  public interface ICollectionView : INotifyPropertyChanged {
    public object ObjectRoot { get; }
    public List<object> ScrollToItem { get; set; }
    public bool IsSizeChanging { get; set; }
    public void Select(object row, object item, bool isCtrlOn, bool isShiftOn);
    public bool SetTopItem(object o);
  }
}
