using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace MH.Utils.BaseClasses {
  public class TreeItem : ListItem, ITreeItem {
    private ITreeItem _parent;

    public ITreeItem Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }
    public ExtObservableCollection<ITreeItem> Items { get; set; } = new();
    public bool IsExpanded {
      get => Bits[BitsMasks.IsExpanded];
      set {
        if (Bits[BitsMasks.IsExpanded] == value) return;
        Bits[BitsMasks.IsExpanded] = value;
        OnIsExpandedChanged(value);
        OnPropertyChanged();
      }
    }
    
    public TreeItem() { }

    public TreeItem(string icon, string name) : base(icon, name) { }

    public TreeItem(ITreeItem parent) {
      Parent = parent;
    }

    public TreeItem(ITreeItem parent, object data) : this(parent) {
      Data = data;
    }

    public virtual void OnIsExpandedChanged(bool value) { }

    public void AddItems(IEnumerable<ITreeItem> items) =>
      Items.AddItems(items.ToList(), item => item.Parent = this);
  }
}
