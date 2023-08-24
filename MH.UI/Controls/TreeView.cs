using MH.UI.Interfaces;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Linq;
using MH.Utils;

namespace MH.UI.Controls {
  public class TreeView : ObservableObject, ITreeView {
    private List<object> _scrollToItems;
    private bool _scrollToTop;
    private int _scrollToIndex = -1;
    private bool _isSizeChanging;

    public ExtObservableCollection<object> RootHolder { get; } = new();
    public List<object> ScrollToItems { get => _scrollToItems; set { _scrollToItems = value; OnPropertyChanged(); } }
    public bool ScrollToTop { get => _scrollToTop; set { _scrollToTop = value; OnPropertyChanged(); } }
    public int ScrollToIndex { get => _scrollToIndex; set { _scrollToIndex = value; OnPropertyChanged(); } }
    public bool IsSizeChanging { get => _isSizeChanging; set => OnSizeChanging(value); }
    public bool IsScrollUnitItem { get; set; } = true;

    public virtual void OnSizeChanging(bool value) {
      _isSizeChanging = value;
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
