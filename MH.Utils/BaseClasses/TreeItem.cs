using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.Utils.BaseClasses {
  public class TreeItem<TP, TI> : ObservableObject, ITreeItem<TP, TI>
    where TP : class, ITreeItem<TP, TI>
    where TI : class, ITreeItem<TP, TI> {

    private TP _parent;
    private bool _isExpanded;
    private bool _isSelected;

    public object Data { get; }
    public TP Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }
    public ExtObservableCollection<TI> Items { get; set; } = new();
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

    public TreeItem(TP parent) {
      Parent = parent;
    }

    public TreeItem(TP parent, object data) : this(parent) {
      Data = data;
    }

    public virtual void OnIsExpandedChanged(bool value) { }

    public static bool IsFullyExpanded(ITreeItem<TP, TI> item) =>
      item.IsExpanded && (item.Parent == null || IsFullyExpanded(item.Parent));

    public void SetExpanded<T>(bool value) where T : ITreeItem<TP, TI> {
      if (IsExpanded != value)
        IsExpanded = value;
      if (Items == null) return;
      foreach (var item in Items.OfType<T>())
        item.SetExpanded<T>(value);
    }

    public static List<T> GetBranch<T>(T item, bool expanded) where T : class, ITreeItem<TP, TI> {
      if (item == null) return null;
      var items = new List<T>();

      while (item != null) {
        items.Add(item);
        if (expanded) item.IsExpanded = true;
        item = item.Parent as T;
      }

      items.Reverse();

      return items;
    }

    public static int GetIndex(ITreeItem<TP, TI> parent, ITreeItem<TP, TI> item) {
      int index = 0;
      bool found = false;
      GetIndex(parent, item, ref index, ref found);
      return found ? index : -1;
    }

    public static void GetIndex(ITreeItem<TP, TI> parent, ITreeItem<TP, TI> item, ref int index, ref bool found) {
      if (ReferenceEquals(parent, item)) {
        found = true;
        return;
      }
      
      if (parent.Items == null) return;

      foreach (var i in parent.Items) {
        index++;
        if (ReferenceEquals(i, item)) {
          found = true;
          break;
        }
        if (!i.IsExpanded) continue;
        GetIndex(i, item, ref index, ref found);
        if (found) break;
      }
    }

    public static T FindItem<T>(IEnumerable<T> items, Func<T, bool> equals) where T : class, ITreeItem<T, T> {
      foreach (var item in items) {
        if (equals(item))
          return item;

        var res = FindItem(item.Items, equals);
        if (res != null) return res;
      }

      return default;
    }

    public IEnumerable<T> GetThisAndParents<T>() where T : ITreeItem<TP, TI> {
      ITreeItem<TP, TI> item = this;
      while (item is T t) {
        yield return t;
        item = item.Parent;
      }
    }

    public void ExpandTo() {
      var items = GetThisAndParents<ITreeItem<TP, TI>>().ToList();

      // don't expand this if Items are empty or it's just placeholder
      if (Items.Count == 0 || Items[0]?.Parent == null)
        items.Remove(this);

      items.Reverse();

      foreach (var item in items)
        item.IsExpanded = true;
    }

    public void AddItem(TI item) {
      Items.Add(item);
      item.Parent = this as TP;
    }

    public void AddItems(IEnumerable<TI> items) {
      foreach (var item in items)
        AddItem(item);
    }

    public bool HasThisParent(ITreeItem parent) {
      var p = Parent;
      while (p != null) {
        if (ReferenceEquals(p, parent))
          return true;
        p = p.Parent;
      }

      return false;
    }
  }

  public class TreeItem : TreeItem<ITreeItem, ITreeItem>, ITreeItem {
    private bool _isHidden;
    private string _iconName;
    private string _name;

    public bool IsHidden { get => _isHidden; set { _isHidden = value; OnPropertyChanged(); } }
    public string IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(GetTitle)); } }
    public string GetTitle => Name;

    public TreeItem() { }

    public TreeItem(string iconName, string name) {
      IconName = iconName;
      Name = name;
    }
  }
}
