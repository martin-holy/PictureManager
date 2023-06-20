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

    public CollectionView<T> View { get; set; }
    public CollectionViewGroup<T> Parent { get; set; }
    public ObservableCollection<T> Source { get; } = new();
    public ObservableCollection<object> Items { get; } = new();
    public CollectionViewGroupByItem<T>[] GroupByItems { get; set; }
    public CollectionViewGroupByItem<T> RecursiveItem { get; set; }
    public object GroupedBy { get; set; }
    public GroupMode GroupMode { get; set; }
    public bool IsGroupedRecursive { get; set; }
    public double Width { get => _width; set => SetWidth(value); }
    public string Icon { get; }
    public string Title { get; }
    // TODO lazy load OnExpanded
    public bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); } }

    public CollectionViewGroup(CollectionViewGroup<T> parent, string icon, string title, object groupedBy, CollectionViewGroupByItem<T> recursiveItem, IEnumerable<T> source) {
      Parent = parent;
      Icon = icon;
      Title = title;
      GroupedBy = groupedBy;
      UpdateSource(source);

      Items.CollectionChanged += (_, e) => {
        if (e.Action is not (NotifyCollectionChangedAction.Reset or NotifyCollectionChangedAction.Remove)
            || e.OldItems == null) return;

        foreach (var g in e.OldItems.OfType<CollectionViewGroup<T>>())
          g.Parent = null;
      };

      if (Parent == null) return;

      View = Parent.View;
      GroupMode = Parent.GroupMode;

      if (Parent.GroupByItems == null) return;

      if (Parent.GroupMode is GroupMode.GroupByRecursive or GroupMode.ThanByRecursive)
        RecursiveItem = recursiveItem ?? Parent.GroupByItems[0];

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
        .Select(item => GetGroups(this, item, false))
        .SelectMany(x => x)
        .OrderBy(x => x.Title);

      Items.Clear();

      foreach (var g in groups) {
        Items.Add(g);
        empty = empty.Except(g.Source).ToArray();

        if (GroupMode == GroupMode.GroupByRecursive)
          GroupRecursive(g);
      }

      if (empty.Length != 0)
        Items.Insert(0, new CollectionViewGroup<T>(this, string.Empty, string.Empty, null, null, empty));
    }

    public void GroupByThenBy() {
      if (GroupByItems == null) return;

      var groups = GetGroups(this, GroupByItems[0], true).ToArray();

      if (groups.Length == 1 && string.IsNullOrEmpty(groups[0].Title)) {
        GroupByItems = null;
        return;
      }

      Items.Clear();

      foreach (var g in groups.OrderBy(x => x.Title)) {
        Items.Add(g);

        if (GroupMode == GroupMode.ThanByRecursive && GroupRecursive(g))
          continue;

        g.GroupByThenBy();
      }
    }

    public static bool GroupRecursive(CollectionViewGroup<T> group) {
      group.IsGroupedRecursive = true;
      var groups = GetGroups(group, group.RecursiveItem, false).ToArray();

      if (groups.Length == 0) {
        group.IsGroupedRecursive = false;
        return false;
      }

      group.Items.Clear();

      foreach (var g in groups.OrderBy(x => x.Title)) {
        group.Items.Add(g);

        if (g.GroupMode is GroupMode.ThanBy or GroupMode.ThanByRecursive)
          g.GroupByThenBy();
      }

      return true;
    }

    public static IEnumerable<CollectionViewGroup<T>> GetGroups(CollectionViewGroup<T> parent, CollectionViewGroupByItem<T> groupBy, bool withEmpty) =>
      parent.Source
        .SelectMany(item => GetGroupBys(item, groupBy, parent.IsGroupedRecursive, withEmpty)
          .Select(group => new { group, item }))
        .GroupBy(x => x.group, x => x.item)
        .Select(x => new CollectionViewGroup<T>(parent, groupBy.IconName, x.Key.Item2, x.Key.Item1, groupBy, x));

    public static IEnumerable<Tuple<object, string>> GetGroupBys(T item, CollectionViewGroupByItem<T> groupBy, bool isRecursive, bool withEmpty) =>
      groupBy.ItemGroupBy(item, groupBy.Parameter, isRecursive)
      ?? (withEmpty
        ? new Tuple<object, string>[] { new(null, string.Empty) }
        : Enumerable.Empty<Tuple<object, string>>());

    public void ReGroupItems(IEnumerable<T> items, bool remove) {
      if (items == null) return;
      var toReWrap = new List<CollectionViewGroup<T>>();
      var toReGroup = new List<CollectionViewGroup<T>>();

      if (remove)
        foreach (var item in items)
          RemoveItem(item, toReWrap);
      else
        foreach (var item in items)
          ReGroupItem(item, toReWrap, toReGroup);

      foreach (var group in toReWrap)
        group.ReWrap();

      foreach (var group in toReGroup)
        group.GroupIt();

      if (toReWrap.Count == 0 && toReGroup.Count == 0)
        return;

      View.ScrollToTopItem();
    }

    public void ReGroupItem(T item, List<CollectionViewGroup<T>> toReWrap, List<CollectionViewGroup<T>> toReGroup) {
      var itemAdded = false;
      var groupByItem = IsGroupedRecursive ? RecursiveItem : GroupByItems?[0];

      // add the item to the source if is not present
      if (!Source.Contains(item)) {
        Source.Add(item);
        itemAdded = true;

        // if the group is not grouped schedule it for ReWrap
        if (groupByItem == null) {
          toReWrap.Add(this);
          return;
        }
      }

      // done if the group is not grouped and the item was already in the source
      if (groupByItem == null) return;

      // find existing group for the item and remove the item from other groups
      var groupBys = GetGroupBys(item, groupByItem, IsGroupedRecursive, true);

      foreach (var gby in groupBys) {
        CollectionViewGroup<T> newGroup = null;

        foreach (var group in Items.OfType<CollectionViewGroup<T>>().ToArray()) {
          if (Equals(gby.Item1, group.GroupedBy))
            newGroup = group;
          else if (!itemAdded)
            group.RemoveItem(item, toReWrap);
        }

        // create new group for the item if it was not found
        if (newGroup == null) {
          newGroup = new(this, groupByItem.IconName, gby.Item2, gby.Item1, null, null);
          Items.SetInOrder(newGroup, x => x is CollectionViewGroup<T> g ? g.Title : string.Empty);
        }

        // reGroup subGroups
        newGroup.ReGroupItem(item, toReWrap, toReGroup);
      }
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

      Source.Sort(View.ItemOrderBy);
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
