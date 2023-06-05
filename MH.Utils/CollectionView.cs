using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MH.Utils {
  public interface ICollectionView {
    public object ObjectRoot { get; }
    public void Select(object row, object item, bool isCtrlOn, bool isShiftOn);
  }

  public class CollectionView<T> : ICollectionView {
    public object ObjectRoot => Root;
    public CollectionViewGroup<T> Root { get; set; }

    public RelayCommand<CollectionViewGroup<T>> OpenGroupByDialogCommand { get; }

    public CollectionView(string icon, string title) {
      Root = new(null, icon, title, null) { View = this };

      OpenGroupByDialogCommand = new(OpenGroupByDialog);
    }

    public virtual int GetItemWidth(object item) => throw new NotImplementedException();
    public virtual IEnumerable<CollectionViewGroupByItem<T>> GetGroupByItems(IEnumerable<T> source) => throw new NotImplementedException();
    public virtual string ItemOrderBy(T item) => throw new NotImplementedException();
    public virtual void Select(IEnumerable<T> source, T item, bool isCtrlOn, bool isShiftOn) => throw new NotImplementedException();

    public void Select(object row, object item, bool isCtrlOn, bool isShiftOn) {
      if (row is not CollectionViewRow<T> r || item is not T i) return;
      Select(r.Group.Source, i, isCtrlOn, isShiftOn);
    }

    private void OpenGroupByDialog(CollectionViewGroup<T> group) {
      var dlg = new GroupByDialog("Chose items to group by", "IconGroup");
      dlg.SetAvailable(GetGroupByItems(group.Source));

      if (Dialog.Show(dlg) != 1) return;

      GroupIt(group, dlg.Chosen.Cast<CollectionViewGroupByItem<T>>().ToList());
    }

    public void GroupIt(CollectionViewGroup<T> group, List<CollectionViewGroupByItem<T>> items, int index = 0) {
      if (index >= items.Count) return;

      group.GroupBy = items[index];
      var groups = group.Source
        .OrderBy(ItemOrderBy)
        .GroupBy(x => group.GroupBy.ItemGroupBy(x, group.GroupBy.Parameter))
        .Select(x => new CollectionViewGroup<T>(group, group.GroupBy.Icon, x.Key, x))
        .OrderBy(x => x.Title)
        .ToArray();

      if (groups.Length == 1 && string.IsNullOrEmpty(groups[0].Title))
        return;

      group.Items.Clear();

      foreach (var g in groups) {
        group.Items.Add(g);
        GroupIt(g, items, index + 1);
      }
    }

    public void ReGroupItems(IEnumerable<T> items) {
      var toReWrap = new List<CollectionViewGroup<T>>();
      var toReGroup = new List<CollectionViewGroup<T>>();

      foreach (var item in items)
        Root.ReGroupItem(item, toReWrap, toReGroup);

      foreach (var group in toReWrap) {
        group.Source.Sort(ItemOrderBy);
        group.ReWrap();
      }

      foreach (var group in toReGroup)
        GroupIt(group, new() { group.GroupBy });
    }
  }

  public class CollectionViewGroup<T> : ObservableObject {
    private bool _isExpanded;
    private double _width;

    public CollectionView<T> View { get; set; }
    public CollectionViewGroup<T> Parent { get; }
    public ObservableCollection<T> Source { get; } = new();
    public ObservableCollection<object> Items { get; } = new();
    public CollectionViewGroupByItem<T> GroupBy { get; set; }
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

    public CollectionViewGroup(CollectionViewGroup<T> parent, string icon, string title, IEnumerable<T> source) {
      Parent = parent;
      Icon = icon;
      Title = title;

      if (Parent != null)
        View = Parent.View;

      UpdateSource(source);
    }

    public void UpdateSource(IEnumerable<T> items) {
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
      if (Items.FirstOrDefault() is CollectionViewGroup<T> || !(Width > 0)) return;

      Items.Clear();

      foreach (var item in Source)
        AddItem(item);
    }

    private void AddItem(object item) {
      CollectionViewRow<T> row = null;

      if (Items.Count > 0)
        row = Items[^1] as CollectionViewRow<T>;

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

    public void ReGroupItem(T item, List<CollectionViewGroup<T>> toReWrap, List<CollectionViewGroup<T>> toReGroup) {
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

      var title = GroupBy.ItemGroupBy(item, GroupBy.Parameter);

      // GroupBy is not null but items are not in groups
      // because there is only one group without Title
      if (Items.FirstOrDefault() is CollectionViewRow<T>) {
        if (string.IsNullOrEmpty(title))
          toReWrap.Add(this);
        else
          toReGroup.Add(this);

        return;
      }

      // find existing group for the item and remove the item from other groups
      CollectionViewGroup<T> newGroup = null;
      foreach (var group in Items.OfType<CollectionViewGroup<T>>().ToArray()) {
        if (group.Icon.Equals(GroupBy.Icon, StringComparison.Ordinal)
            && group.Title.Equals(title, StringComparison.CurrentCulture))
          newGroup = group;
        else
          group.RemoveItem(item, toReWrap);
      }

      // create new group for the item if it was not found
      if (newGroup == null) {
        newGroup = new(this, GroupBy.Icon, title, null);
        Items.SetInOrder(newGroup, x => x is CollectionViewGroup<T> g ? g.Title : string.Empty);
      }

      // reGroup subGroups
      newGroup.ReGroupItem(item, toReWrap, toReGroup);
    }

    public void RemoveItem(T item, List<CollectionViewGroup<T>> toReWrap) {
      if (!Source.Remove(item)) return;

      // remove the Group from its Parent if it is empty
      if (Source.Count == 0) {
        Parent?.Items.Remove(this);

        // reGroup the Parent if it has only one Group without Title
        if (Parent?.Items.Count == 1
            && Parent.Items[0] is CollectionViewGroup<T> g
            && string.IsNullOrEmpty(g.Title)) {
          Parent.Items.Clear();
          toReWrap.Add(Parent);
          return;
        }
      }

      // schedule the Group for reWrap if doesn't have any subGroups
      if (Items.FirstOrDefault() is CollectionViewRow<T>) {
        toReWrap.Add(this);
        return;
      }

      // remove the Item from subGroups
      foreach (var group in Items.OfType<CollectionViewGroup<T>>())
        group.RemoveItem(item, toReWrap);
    }
  }

  public class CollectionViewGroupByItem<T> {
    public string Icon { get; }
    public string Title { get; }
    public object Parameter { get; }
    public Func<T, object, string> ItemGroupBy { get; }

    public CollectionViewGroupByItem(string icon, string title, object parameter, Func<T, object, string> itemGroupBy) {
      Icon = icon;
      Title = title;
      Parameter = parameter;
      ItemGroupBy = itemGroupBy;
    }
  }

  public class CollectionViewRow<T> {
    public CollectionViewGroup<T> Group { get; }
    public ObservableCollection<object> Items { get; } = new();
    public bool IsExpanded { get; set; } = true;

    public CollectionViewRow(CollectionViewGroup<T> group) {
      Group = group;
    }
  }
}
