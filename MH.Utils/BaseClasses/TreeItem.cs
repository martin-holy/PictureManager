using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace MH.Utils.BaseClasses {
  public class TreeItem : ObservableObject, ITreeItem {
    // TODO move this to other class
    #region Move this
    private bool _isHidden;
    private string _iconName;
    private string _name;

    public bool IsHidden { get => _isHidden; set { _isHidden = value; OnPropertyChanged(); } }
    public string IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(GetTitle)); } }
    public string GetTitle => Name;

    public TreeItem(string iconName, string name) {
      IconName = iconName;
      Name = name;
    }
    #endregion
    
    private ITreeItem _parent;
    private bool _isExpanded;
    private bool _isSelected;

    public object Data { get; }
    public ITreeItem Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }
    public ExtObservableCollection<ITreeItem> Items { get; set; } = new();
    public bool IsExpanded {
      get => _isExpanded;
      set {
        _isExpanded = value;
        OnIsExpandedChanged(value);
        OnPropertyChanged();
      }
    }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }

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
