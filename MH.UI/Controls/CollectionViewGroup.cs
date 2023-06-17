using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace MH.UI.Controls {
  public enum GroupMode {
    GroupBy,
    GroupByRecursive,
    ThanBy,
    ThanByRecursive
  }

  public class CollectionViewGroup<T> : ObservableObject {
    private bool _isExpanded;
    private double _width;
    private bool _modeGroupBy = true;
    private bool _modeGroupByThenBy;
    private bool _modeGroupRecursive;

    public CollectionView<T> View { get; set; }
    public CollectionViewGroup<T> Parent { get; set; }
    public ObservableCollection<T> Source { get; } = new();
    public ObservableCollection<object> Items { get; } = new();
    public CollectionViewGroupByItem<T>[] GroupByItems { get; set; }
    public CollectionViewGroupByItem<T> RecursiveItem { get; set; }
    public GroupMode GroupMode { get; set; }
    public bool IsGroupedRecursive { get; set; }
    public bool ModeGroupBy { get => _modeGroupBy; set { _modeGroupBy = value; OnPropertyChanged(); } }
    public bool ModeGroupByThenBy { get => _modeGroupByThenBy; set { _modeGroupByThenBy = value; OnPropertyChanged(); } }
    public bool ModeGroupRecursive { get => _modeGroupRecursive; set { _modeGroupRecursive = value; OnPropertyChanged(); } }
    public double Width { get => _width; set => SetWidth(value); }
    public string Icon { get; }
    public string Title { get; }
    // TODO lazy load OnExpanded
    public bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); } }

    /* Modes
     * GroupBy:
     *  - grouped by all items
     *  - nothing is set to sub groups
     * GroupBy with recursive:
     *  - grouped by all items
     *  - group sub groups recursive (where to store item?)
     * GroupByThanBy:
     *  - group by first item
     *  - send next items to sub groups
     * GroupByThanBy with recursive:
     *  - group by first item
     *  - group sub groups recursive (where to store item?)
     *  - send next items to sub groups or recursive sub groups
     *
     *
     * try to store not [1..] for sub group, but all and use the first one for recursive group
     * and the rest for thanBy
     */

    public CollectionViewGroup(CollectionViewGroup<T> parent, CollectionViewGroupByItem<T> recursiveItem, string icon, string title, IEnumerable<T> source) {
      Parent = parent;
      Icon = icon;
      Title = title;

      Init(recursiveItem);
      UpdateSource(source);

      Items.CollectionChanged += (_, e) => {
        if (e.Action is not (NotifyCollectionChangedAction.Reset or NotifyCollectionChangedAction.Remove) ||
            e.OldItems == null) return;

        foreach (var group in e.OldItems.OfType<CollectionViewGroup<T>>())
          group.Parent = null;
      };
    }

    private void Init(CollectionViewGroupByItem<T> recursiveItem) {
      if (Parent == null) return;

      View = Parent.View;
      GroupMode = Parent.GroupMode;

      if (Parent.GroupByItems == null)
        return;

      if (recursiveItem != null)
        RecursiveItem = recursiveItem;
      else if (Parent.GroupMode is GroupMode.GroupByRecursive or GroupMode.ThanByRecursive)
        RecursiveItem = Parent.GroupByItems[0];

      if (Parent.GroupMode is GroupMode.ThanBy or GroupMode.ThanByRecursive) {
        if (Parent.IsGroupedRecursive)
          GroupByItems = Parent.GroupByItems.ToArray();
        else if (Parent.GroupByItems.Length > 1)
          GroupByItems = Parent.GroupByItems[1..];
      }
    }

    public void GroupIt() {
      switch (GroupMode) {
        case GroupMode.GroupBy or GroupMode.GroupByRecursive:
          GroupBy();
          break;
        case GroupMode.ThanBy or GroupMode.ThanByRecursive:
          GroupByThenBy();
          break;
      }
    }

    public void GroupBy() {
      var empty = Source.ToArray();
      var groups = GroupByItems
        .Select(item => Source
          .OrderBy(View.ItemOrderBy)
          .GroupBy(x => item.ItemGroupBy(x, item.Parameter))
          .Where(x => !string.IsNullOrEmpty(x.Key))
          .Select(x => new CollectionViewGroup<T>(this, GroupMode == GroupMode.GroupByRecursive ? item : null, item.IconName, x.Key, x))
          .ToArray())
        .SelectMany(x => x)
        .OrderBy(x => x.Title);

      Items.Clear();

      foreach (var g in groups) {
        empty = empty.Except(g.Source).ToArray();
        Items.Add(g);

        if (GroupMode == GroupMode.GroupByRecursive)
          View.GroupRecursive(g);
      }

      if (empty.Length != 0)
        Items.Insert(0, new CollectionViewGroup<T>(this, null, string.Empty, string.Empty, empty));
    }

    public void GroupByThenBy() {
      // TODO remove GroupByItems.Length == 0, it should by null
      if (GroupByItems == null) return;

      if (GroupByItems.Length == 0)
        return;

      var groups = Source
        .OrderBy(View.ItemOrderBy)
        .GroupBy(x => GroupByItems[0].ItemGroupBy(x, GroupByItems[0].Parameter))
        .Select(x => new CollectionViewGroup<T>(this, null, GroupByItems[0].IconName, x.Key, x))
        .OrderBy(x => x.Title)
        .ToArray();

      if (groups.Length == 1 && string.IsNullOrEmpty(groups[0].Title)) {
        GroupByItems = null;
        return;
      }

      Items.Clear();

      foreach (var g in groups) {
        Items.Add(g);

        if (GroupMode == GroupMode.ThanByRecursive && View.GroupRecursive(g))
          continue;

        g.GroupByThenBy();
      }
    }

    // BUG maybe root is not regrouped when changing person group or there is no event after personGroupChanged
    public void ReGroupItems(IEnumerable<T> items) {
      var toReWrap = new List<CollectionViewGroup<T>>();
      var toReGroup = new List<CollectionViewGroup<T>>();

      foreach (var item in items)
        ReGroupItem(item, toReWrap, toReGroup);

      foreach (var group in toReWrap) {
        group.Source.Sort(View.ItemOrderBy);
        group.ReWrap();
      }

      foreach (var group in toReGroup)
        group.GroupIt();

      if (toReWrap.Count == 0 && toReGroup.Count == 0)
        return;

      View.ScrollToTopItem();
    }

    public void ReGroupItem(T item, List<CollectionViewGroup<T>> toReWrap, List<CollectionViewGroup<T>> toReGroup) {
      // add the item to the source if is not present
      if (!Source.Contains(item)) {
        Source.Add(item);

        // if the group is not grouped schedule it for ReWrap
        if (GroupByItems == null) {
          toReWrap.Add(this);
          return;
        }
      }

      // done if the group is not grouped and the item was already in the source
      if (GroupByItems == null) return;

      // find existing group for the item and remove the item from other groups
      var title = GroupByItems[0].ItemGroupBy(item, GroupByItems[0].Parameter);
      CollectionViewGroup<T> newGroup = null;
      foreach (var group in Items.OfType<CollectionViewGroup<T>>().ToArray()) {
        if (group.Icon.Equals(GroupByItems[0].IconName, StringComparison.Ordinal)
            && group.Title.Equals(title, StringComparison.CurrentCulture))
          newGroup = group;
        else
          group.RemoveItem(item, toReWrap);
      }

      // create new group for the item if it was not found
      if (newGroup == null) {
        newGroup = new(this, null, GroupByItems[0].IconName, title, null);
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
        return;
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

    // TODO AddItems(IEnumerable<T> items)
    private void AddItem(T item) {
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
  }
}
