using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils.Dialogs;

namespace MH.Utils {
  public class CollectionView {
    public CollectionViewGroup Root { get; set; }
    public virtual int GetItemWidth(object item) => throw new NotImplementedException();
    public virtual IEnumerable<object> GetGroupByItems(IEnumerable<object> source) => throw new NotImplementedException();
    public virtual IOrderedEnumerable<CollectionViewGroup> GroupByItem(CollectionViewGroup group, ListItem<object> item) => throw new NotImplementedException();
    public virtual void Select(IEnumerable<object> source, object item, bool isCtrlOn, bool isShiftOn) => throw new NotImplementedException();

    public RelayCommand<CollectionViewGroup> OpenGroupByDialogCommand { get; }

    public CollectionView() {
      OpenGroupByDialogCommand = new(OpenGroupByDialog);
    }

    private void OpenGroupByDialog(CollectionViewGroup group) {
      var dlg = new GroupByDialog("Chose items to group by", "IconGroup");
      dlg.SetAvailable(GetGroupByItems(group.Source));

      if (Dialog.Show(dlg) != 1) return;

      GroupIt(group, dlg.Chosen.Cast<ListItem<object>>().ToList());
    }

    public void GroupIt(CollectionViewGroup group, List<ListItem<object>> items, int index = 0) {
      if (index >= items.Count) return;

      var item = items[index];
      var groups = GroupByItem(group, item).ToArray();

      if (groups.Length == 1 && string.IsNullOrEmpty(groups[0].Title))
        return;

      group.WrappedItems.Clear();

      foreach (var g in groups) {
        group.WrappedItems.Add(g);
        GroupIt(g, items, index + 1);
      }
    }
  }

  public class CollectionViewRow {
    public CollectionViewGroup Group { get; }
    public ObservableCollection<object> Items { get; } = new();
    public bool IsExpanded { get; set; } = true;

    public CollectionViewRow(CollectionViewGroup group) {
      Group = group;
    }
  }

  public class CollectionViewGroup : ObservableObject {
    private bool _isExpanded;

    public CollectionView View { get; set; }
    public CollectionViewGroup Parent { get; }
    public ObservableCollection<object> Source { get; } = new();
    public ObservableCollection<object> WrappedItems { get; } = new();
    public double Width { get; set; }
    public string Icon { get; }
    public string Title { get; }

    public bool IsExpanded {
      get => _isExpanded;
      set {
        _isExpanded = value;
        ExpandedChangedEvent(this, EventArgs.Empty);
        OnPropertyChanged();
      }
    }

    public RelayCommand<double> SizeChangedCommand { get; }
    public event EventHandler ExpandedChangedEvent = delegate { };

    public CollectionViewGroup(CollectionViewGroup parent, string icon, string title, IEnumerable<object> source) {
      Parent = parent;
      Icon = icon;
      Title = title;

      if (Parent != null)
        View = Parent.View;

      SizeChangedCommand = new(OnWidthChanged);
      UpdateSource(source);
    }

    public void UpdateSource(IEnumerable<object> items) {
      if (items == null) return;

      Source.Clear();
      foreach (var item in items)
        Source.Add(item);
    }

    public void OnWidthChanged(double width) {
      Width = width;
      ReWrap();
    }

    public void ReWrap() {
      if (WrappedItems.FirstOrDefault() is CollectionViewGroup || !(Width > 0)) return;

      WrappedItems.Clear();

      foreach (var item in Source)
        AddItem(item);
    }

    private void AddItem(object item) {
      CollectionViewRow row = null;

      if (WrappedItems.Count > 0)
        row = WrappedItems[^1] as CollectionViewRow;

      if (row == null) {
        row = new(this);
        WrappedItems.Add(row);
      }

      var usedSpace = row.Items.Sum(x => View.GetItemWidth(x));
      var itemWidth = View.GetItemWidth(item);

      if (Width - usedSpace < itemWidth) {
        row = new(this);
        WrappedItems.Add(row);
      }

      row.Items.Add(item);
    }
  }
}
