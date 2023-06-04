using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MH.Utils {
  public class CollectionView {
    public CollectionViewGroup Root { get; set; }

    public virtual int GetItemWidth(object item) => throw new NotImplementedException();
    public virtual IEnumerable<object> GetGroupByItems(IEnumerable<object> source) => throw new NotImplementedException();
    public virtual IOrderedEnumerable<CollectionViewGroup> SourceGroupBy(CollectionViewGroup group) => throw new NotImplementedException();
    public virtual string ItemGroupBy(CollectionViewGroup group, object item) => throw new NotImplementedException();
    public virtual string ItemOrderBy(object item) => throw new NotImplementedException();
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

      group.GroupBy = items[index];
      var groups = SourceGroupBy(group).ToArray();

      if (groups.Length == 1 && string.IsNullOrEmpty(groups[0].Title))
        return;

      group.Items.Clear();

      foreach (var g in groups) {
        group.Items.Add(g);
        GroupIt(g, items, index + 1);
      }
    }

    public void ReGroupItems(IEnumerable<object> items) {
      var toReWrap = new List<CollectionViewGroup>();
      var toReGroup = new List<CollectionViewGroup>();

      foreach (var item in items)
        Root.ReGroupItem(item, toReWrap, toReGroup);


      // TODO do not sort until now, sort everything affected before re wrap 

      foreach (var group in toReWrap)
        group.ReWrap();

      foreach (var group in toReGroup)
        GroupIt(group, new() { group.GroupBy });
    }
  }

  public class CollectionViewGroup : ObservableObject {
    private bool _isExpanded;
    private double _width;

    public CollectionView View { get; set; }
    public CollectionViewGroup Parent { get; }
    public ObservableCollection<object> Source { get; } = new();
    public ObservableCollection<object> Items { get; } = new();
    public ListItem<object> GroupBy { get; set; }
    public double Width { get => _width; set => SetWidth(value); }
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

    public event EventHandler ExpandedChangedEvent = delegate { };

    public CollectionViewGroup(CollectionViewGroup parent, string icon, string title, IEnumerable<object> source) {
      Parent = parent;
      Icon = icon;
      Title = title;

      if (Parent != null)
        View = Parent.View;

      UpdateSource(source);
    }

    public void UpdateSource(IEnumerable<object> items) {
      if (items == null) return;

      Source.Clear();
      foreach (var item in items)
        Source.Add(item);
    }

    private void SetWidth(double width) {
      if (Math.Abs(Width - width) < 1) return;
      _width = width;
      ReWrap();
    }

    public void ReWrap() {
      if (Items.FirstOrDefault() is CollectionViewGroup || !(Width > 0)) return;

      Items.Clear();

      foreach (var item in Source)
        AddItem(item);
    }

    private void AddItem(object item) {
      CollectionViewRow row = null;

      if (Items.Count > 0)
        row = Items[^1] as CollectionViewRow;

      if (row == null) {
        row = new(this);
        Items.Add(row);
      }

      var usedSpace = row.Items.Sum(x => View.GetItemWidth(x));
      var itemWidth = View.GetItemWidth(item);

      if (Width - usedSpace < itemWidth) {
        row = new(this);
        Items.Add(row);
      }

      row.Items.Add(item);
    }

    public void ReGroupItem(object item, List<CollectionViewGroup> toReWrap, List<CollectionViewGroup> toReGroup) {
      // add an item to the Source if is not present
      if (!Source.Contains(item)) {
        Source.Add(item);

        // if the Group is not grouped schedule it for ReWrap
        if (GroupBy == null) {
          toReWrap.Add(this);
          return;
        }
      }

      // done if the Group is not grouped and an item was already in the Source
      if (GroupBy == null) return;

      var title = View.ItemGroupBy(this, item);

      // GroupBy is not null but items are not in groups
      // because there is only one group without Title
      if (Items.FirstOrDefault() is CollectionViewRow) {
        if (string.IsNullOrEmpty(title))
          toReWrap.Add(this);
        else
          toReGroup.Add(this);

        return;
      }

      // find existing group for the item and remove the item from other groups
      CollectionViewGroup newGroup = null;
      foreach (var group in Items.OfType<CollectionViewGroup>().ToArray()) {
        if (group.Icon.Equals(GroupBy.IconName, StringComparison.Ordinal)
            && group.Title.Equals(title, StringComparison.CurrentCulture))
          newGroup = group;
        else
          group.RemoveItem(item, toReWrap);
      }

      // create new group for the item if it was not found
      if (newGroup == null) {
        newGroup = new(this, GroupBy.IconName, title, null);
        Items.SetInOrder(newGroup, x => x is CollectionViewGroup g ? g.Title : string.Empty);
      }

      // reGroup subGroups
      newGroup.ReGroupItem(item, toReWrap, toReGroup);
    }

    public void RemoveItem(object item, List<CollectionViewGroup> toReWrap) {
      if (!Source.Remove(item)) return;

      // remove the Group from its Parent if it is empty
      if (Source.Count == 0) {
        Parent?.Items.Remove(this);

        // reGroup the Parent if it has only one Group without Title
        if (Parent?.Items.Count == 1
            && Parent.Items[0] is CollectionViewGroup g
            && string.IsNullOrEmpty(g.Title)) {
          Parent.Items.Clear();
          toReWrap.Add(Parent);
          return;
        }
      }

      // schedule the Group for reWrap if doesn't have any subGroups
      if (Items.FirstOrDefault() is CollectionViewRow) {
        toReWrap.Add(this);
        return;
      }

      // remove the Item from subGroups
      foreach (var group in Items.OfType<CollectionViewGroup>())
        group.RemoveItem(item, toReWrap);
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
}
