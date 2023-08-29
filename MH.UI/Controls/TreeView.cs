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
    private List<object> _scrollToItems;
    private bool _scrollToTop;
    private int _scrollToIndex = -1;
    private bool _isSizeChanging;

    public ExtObservableCollection<object> RootHolder { get; } = new();
    public Selecting<T> SelectedTreeItems { get; } = new();
    public List<object> ScrollToItems { get => _scrollToItems; set { _scrollToItems = value; OnPropertyChanged(); } }
    public bool ScrollToTop { get => _scrollToTop; set { _scrollToTop = value; OnPropertyChanged(); } }
    public int ScrollToIndex { get => _scrollToIndex; set { _scrollToIndex = value; OnPropertyChanged(); } }
    public bool IsSizeChanging { get => _isSizeChanging; set => OnSizeChanging(value); }
    public bool IsScrollUnitItem { get; set; } = true;
    // TODO rename and combine with single and multi select
    public bool ShowTreeItemSelection { get; set; }

    public event EventHandler<ObjectEventArgs<T>> TreeItemSelectedEvent = delegate { };

    public virtual void OnSizeChanging(bool value) {
      _isSizeChanging = value;
    }

    public virtual void OnTreeItemSelected(object o) {
      if (o is not T t) return;
      TreeItemSelectedEvent(this, new(t));
      if (!ShowTreeItemSelection) return;
      SelectedTreeItems.Select(t.Parent?.Items.Cast<T>().ToList(), t, Keyboard.IsCtrlOn(), Keyboard.IsShiftOn());
    }

    public virtual bool SetTopItem(object o) => true;

    public virtual void ScrollTo(ITreeItem item) {
      var items = item.GetBranch(true);
      if (items == null) return;

      if (IsScrollUnitItem && RootHolder.Count != 0 && RootHolder[0] is ITreeItem root)
        ScrollToIndex = item.GetIndex(root);

      ScrollToItems = items.Cast<object>().ToList();
    }
  }
}
