using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace MH.Utils.BaseClasses {
  public class TreeItemBase<TP, TI, TL> : ObservableObject, ITreeItemBase<TP, TI, TL>
    where TP : ITreeItemBase<TP, TI, TL>
    where TI : ITreeItemBase<TP, TI, TL> {

    private TP _parent;
    private bool _isExpanded;

    public TP Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }
    public ExtObservableCollection<TI> Items { get; set; } = new();
    public ExtObservableCollection<TL> Leaves { get; set; } = new();
    public bool IsExpanded {
      get => _isExpanded;
      set {
        _isExpanded = value;
        OnIsExpandedChanged(value);
        OnPropertyChanged();
      }
    }

    public TreeItemBase(TP parent) {
      Parent = parent;
    }

    public virtual void OnIsExpandedChanged(bool value) { }

    public static bool IsFullyExpanded(ITreeItemBase<TP, TI, TL> item) =>
      item.IsExpanded && (item.Parent == null || IsFullyExpanded(item.Parent));

    public void SetExpanded<T>(bool value) where T : ITreeItemBase<TP, TI, TL> {
      if (IsExpanded != value)
        IsExpanded = value;
      if (Items == null) return;
      foreach (var item in Items.OfType<T>())
        item.SetExpanded<T>(value);
    }

    public static List<T> GetBranch<T>(T item, bool expanded)
      where T : class, ITreeItemBase<TP, TI, TL> {

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

    public static int GetIndex(ITreeItemBase<TP, TI, TL> parent, ITreeItemBase<TP, TI, TL> item) {
      int index = 0;
      bool found = false;
      GetIndex(parent, item, ref index, ref found);
      return found ? index : -1;
    }

    public static void GetIndex(ITreeItemBase<TP, TI, TL> parent, ITreeItemBase<TP, TI, TL> item, ref int index, ref bool found) {
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
  }
}
