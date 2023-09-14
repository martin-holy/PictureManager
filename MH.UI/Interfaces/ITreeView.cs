using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace MH.UI.Interfaces {
  public interface ITreeView : INotifyPropertyChanged {
    public ExtObservableCollection<object> RootHolder { get; }
    public bool IsSizeChanging { get; set; }
    public bool ShowTreeItemSelection { get; set; }
    public ITreeItem TopTreeItem { get; set; }
    public Action ScrollToTopAction { get; set; }
    public Action<IEnumerable<object>> ScrollToItemsAction { get; set; }
    public RelayCommand<object> TreeItemSelectedCommand { get; }
  }
}
