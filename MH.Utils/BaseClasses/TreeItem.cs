using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MH.Utils.BaseClasses {
  public class TreeItem : ListItem, ITreeItem {
    private ITreeItem _parent;
    private bool _isExpanded;

    public ObservableCollection<ITreeItem> Items { get; set; } = new();
    public ITreeItem Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }
    public bool IsExpanded {
      get => _isExpanded;
      set {
        _isExpanded = value;
        ExpandedChangedEventHandler(this, EventArgs.Empty);
        OnPropertyChanged();
      }
    }

    public event EventHandler ExpandedChangedEventHandler = delegate { };

    protected TreeItem() { }
    protected TreeItem(string iconName, string name) : base(iconName, name) { }

    public bool HasThisParent(ITreeItem parent) {
      var p = Parent;
      while (p != null) {
        if (p.Equals(parent))
          return true;
        p = p.Parent;
      }

      return false;
    }

    public void ExpandTo() {
      var items = new List<ITreeItem>();
      Tree.GetThisAndParentRecursive(this, ref items);

      // don't expand this if Items are empty or it's just placeholder
      if (Items.Count == 0 || Items[0]?.Parent == null)
        items.Remove(this);

      items.Reverse();

      foreach (var item in items)
        item.IsExpanded = true;
    }

    public void ExpandAll() {
      if (Items.Count == 0) return;
      IsExpanded = true;
      foreach (var item in Items.OfType<ITreeItem>())
        item.ExpandAll();
    }
  }
}
