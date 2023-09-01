using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MH.UI.Interfaces {
  public interface ITreeView : INotifyPropertyChanged {
    public ExtObservableCollection<object> RootHolder { get; }
    public bool IsScrollUnitItem { get; set; }
    public bool IsSizeChanging { get; set; }
    public bool ShowTreeItemSelection { get; set; }
    public bool SetTopItem(object o);
    public Action ScrollToTopAction { get; set; }
    public Action<IEnumerable<object>, int?> ScrollToItemsAction { get; set; }
    public RelayCommand<object> TreeItemSelectedCommand { get; }
  }
}
