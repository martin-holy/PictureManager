using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MH.Utils.BaseClasses {
  public class TreeItem : ObservableObject, ITreeItem {
    // TODO move this to other class
    #region Move this
    private string _iconName;
    private string _name;

    public string IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(GetTitle)); } }
    public string GetTitle => Name;

    public TreeItem(string iconName, string name) {
      IconName = iconName;
      Name = name;
    }
    #endregion

    private BitVector32 _bits = new(0);
    private ITreeItem _parent;

    public object Data { get; }
    public ITreeItem Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }
    public ExtObservableCollection<ITreeItem> Items { get; set; } = new();
    public bool IsSelected { get => _bits[BitsMasks.IsSelected]; set { _bits[BitsMasks.IsSelected] = value; OnPropertyChanged(); } }
    public bool IsHidden { get => _bits[BitsMasks.IsHidden]; set { _bits[BitsMasks.IsHidden] = value; OnPropertyChanged(); } }
    public bool IsExpanded {
      get => _bits[BitsMasks.IsExpanded];
      set {
        _bits[BitsMasks.IsExpanded] = value;
        OnIsExpandedChanged(value);
        OnPropertyChanged();
      }
    }
    
    public TreeItem() { }

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
