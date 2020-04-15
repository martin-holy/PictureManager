using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace PictureManager.ViewModel {
  public class BaseTreeViewItem : INotifyPropertyChanged {
    public ObservableCollection<BaseTreeViewItem> Items { get; set; } = new ObservableCollection<BaseTreeViewItem>();
    public object Tag { get; set; }

    private bool _isExpanded;
    private bool _isSelected;
    private string _title;
    private IconName _iconName;
    private string _toolTip;
    private BackgroundBrush _backgroundBrush;
    private BaseTreeViewItem _parent;
    private readonly object _itemsLock = new object();

    public virtual bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); } }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public virtual string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public IconName IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string ToolTip { get => _toolTip; set { _toolTip = value; OnPropertyChanged(); } }
    public BackgroundBrush BackgroundBrush { get => _backgroundBrush; set { _backgroundBrush = value; OnPropertyChanged(); } }
    public BaseTreeViewItem Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public BaseTreeViewItem() {
      BindingOperations.CollectionRegistering += delegate(object o, CollectionRegisteringEventArgs e) {
        if (Equals(e.Collection, Items))
          BindingOperations.EnableCollectionSynchronization(Items, _itemsLock);
      };
    }

    public BaseTreeViewItem GetTopParent() {
      return Parent == null ? this : Parent.GetTopParent();
    }

    public void GetThisAndItemsRecursive(ref List<BaseTreeViewItem> items) {
      items.Add(this);
      foreach (var item in Items)
        item.GetThisAndItemsRecursive(ref items);
    }

    public void GetThisAndParentRecursive(ref List<BaseTreeViewItem> items) {
      items.Add(this);
      Parent?.GetThisAndParentRecursive(ref items);
    }

    public static void ExpandTo(BaseTreeViewItem item) {
      // expand item as well if it has any sub item and not just placeholder
      if (item.Items.Count > 0 && item.Items[0].Title != null)
        item.IsExpanded = true;
      var parent = item.Parent;
      while (parent != null) {
        parent.IsExpanded = true;
        parent = parent.Parent;
      }
    }

    public string GetFullPath(string separator) {
      var parent = Parent;
      var names = new List<string> { Title };
      while (parent != null) {
        names.Add(parent.Title);
        parent = parent.Parent;
        if (parent is BaseCategoryItem) break;
      }
      names.Reverse();

      return string.Join(separator, names);
    }
  }
}
