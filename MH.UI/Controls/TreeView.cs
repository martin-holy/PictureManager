using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls {
  public class TreeView<T> : ObservableObject, ITreeView where T : ITreeItem {
    private bool _isSizeChanging;

    public ExtObservableCollection<object> RootHolder { get; } = new();
    public Selecting<T> SelectedTreeItems { get; } = new();
    public ITreeItem TopItem { get; set; }
    public bool IsSizeChanging { get => _isSizeChanging; set { _isSizeChanging = value; OnSizeChanging(value); } }
    public bool IsScrollUnitItem { get; set; }
    // TODO rename and combine with single and multi select
    public bool ShowTreeItemSelection { get; set; }
    public Action ScrollToTopAction { get; set; }
    public Action<IEnumerable<object>, int?> ScrollToItemsAction { get; set; }

    public RelayCommand<object> TreeItemSelectedCommand { get; }
    public event EventHandler<ObjectEventArgs<T>> TreeItemSelectedEvent = delegate { };

    public TreeView() {
      TreeItemSelectedCommand = new(OnTreeItemSelected);
    }

    public virtual void OnSizeChanging(bool value) {
      if (!value) ScrollTo(TopItem);
    }

    public virtual void OnTreeItemSelected(object o) {
      if (o is not T t) return;
      TreeItemSelectedEvent(this, new(t));
      if (!ShowTreeItemSelection) return;
      SelectedTreeItems.Select(t.Parent?.Items.Cast<T>().ToList(), t, Keyboard.IsCtrlOn(), Keyboard.IsShiftOn());
    }

    public virtual bool SetTopItem(object o) {
      TopItem = o as ITreeItem;
      return TopItem != null;
    }

    public virtual void ScrollTo(ITreeItem item) {
      var branch = item.GetBranch();
      int? index = !IsScrollUnitItem
        ? null
        : RootHolder is [ITreeItem root]
          ? item.GetIndex(root)
          : -1;

      for (int i = 0; i < branch.Count - 1; i++)
        branch[i].IsExpanded = true;

      ScrollToItemsAction?.Invoke(branch, index);
    }
  }
}
