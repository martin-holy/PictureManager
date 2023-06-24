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
    public ObservableCollection<T> Source { get; }
    public ObservableCollection<object> Items { get; } = new();
    public CollectionViewGroupByItem<T>[] GroupByItems { get; set; }
    public CollectionViewGroupByItem<T> GroupedBy { get; set; }
    public GroupMode GroupMode { get; set; }
    public double Width { get => _width; set => SetWidth(value); }
    public string Icon { get; set; }
    public string Title { get; set; }
    // TODO lazy load OnExpanded
    public bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); } }

    public CollectionViewGroup(CollectionViewGroup<T> parent, CollectionViewGroupByItem<T> groupedBy, ObservableCollection<T> source) {
      Parent = parent;
      Icon = groupedBy == null ? string.Empty : groupedBy.IconName;
      Title = groupedBy == null ? string.Empty : groupedBy.Name;
      GroupedBy = groupedBy;
      Source = source;

      Items.CollectionChanged += (_, e) => {
        if (e.Action is not (NotifyCollectionChangedAction.Reset or NotifyCollectionChangedAction.Remove)
            || e.OldItems == null) return;

        foreach (var g in e.OldItems.OfType<CollectionViewGroup<T>>())
          g.Parent = null;
      };

      if (Parent == null) return;

      View = Parent.View;
      GroupMode = Parent.GroupMode;

      if (Parent.GroupByItems == null
          || GroupMode is not (GroupMode.ThanBy or GroupMode.ThanByRecursive)) return;

      if (GroupMode == GroupMode.ThanByRecursive && Parent.GroupedBy?.Items?.Count > 0)
        GroupByItems = Parent.GroupByItems.ToArray();
      else if (Parent.GroupByItems.Length > 1)
        GroupByItems = Parent.GroupByItems[1..];
    }

    public void GroupIt() {
      switch (GroupMode) {
        case GroupMode.GroupBy or GroupMode.GroupByRecursive:
          CreateGroups(this, GroupByItems, true);
          break;
        case GroupMode.ThanBy or GroupMode.ThanByRecursive:
          GroupByThenBy();
          break;
      }
    }

    public static CollectionViewGroup<T> CreateGroup(CollectionViewGroup<T> parent, CollectionViewGroupByItem<T> groupBy, List<T> notInGroup) {
      var source = new ObservableCollection<T>();

      foreach (var item in parent.Source)
        if (groupBy.ItemGroupBy(item, groupBy.Parameter)) {
          source.Add(item);
          notInGroup.Remove(item);
        }

      if (source.Count == 0) return null;

      var group = new CollectionViewGroup<T>(parent, groupBy, source);
      parent.Items.Add(group);

      return group;
    }

    public static void CreateGroups(CollectionViewGroup<T> parent, IEnumerable<CollectionViewGroupByItem<T>> groupBys, bool withEmpty) {
      var notInGroups = parent.Source.ToList();
      parent.Items.Clear();

      foreach (var gbi in groupBys) {
        var group = CreateGroup(parent, gbi, notInGroups);
        if (group == null) continue;
        if (parent.GroupMode is GroupMode.GroupByRecursive or GroupMode.ThanByRecursive && gbi.Items?.Count > 0)
          CreateGroups(group, gbi.Items.Cast<CollectionViewGroupByItem<T>>(), false);
      }

      if (!withEmpty || notInGroups.Count == 0) return;
      var collection = new ObservableCollection<T>();
      foreach (var item in notInGroups)
        collection.Add(item);

      parent.Items.Insert(0, new CollectionViewGroup<T>(parent, null, collection));
    }

    public void GroupByThenBy() {
      if (GroupByItems == null) {
        if (GroupMode == GroupMode.ThanByRecursive && GroupedBy?.Items?.Count > 0)
          CreateGroups(this, GroupedBy.Items.Cast<CollectionViewGroupByItem<T>>(), false);

        return;
      }

      CreateGroups(this, new[] { GroupByItems[0] }, true);

      foreach (var group in Items.OfType<CollectionViewGroup<T>>()) {
        if (GroupMode == GroupMode.ThanByRecursive) {
          var subGroups = group.Items.OfType<CollectionViewGroup<T>>().ToArray();

          if (subGroups.Length > 0) {
            foreach (var subGroup in subGroups)
              subGroup.GroupByThenBy();

            continue;
          }
        }

        group.GroupByThenBy();
      }
    }

    public void ReGroupItems(IEnumerable<T> items, bool remove) {
      if (items == null) return;
      var toReWrap = new List<CollectionViewGroup<T>>();

      if (remove)
        foreach (var item in items)
          RemoveItem(item, toReWrap);
      else
        foreach (var item in items)
          ReGroupItem(item, toReWrap);

      foreach (var group in toReWrap)
        group.ReWrap();

      if (toReWrap.Count == 0) return;

      if (toReWrap.Any(IsFullyExpanded))
      View.ScrollToTopItem();
    }

    public void ReGroupItem(T item, List<CollectionViewGroup<T>> toReWrap) {
      var itemAdded = false;
      var isGrouping = GroupByItems != null || GroupedBy?.Items?.Count > 0;

      // add the item to the source if is not present
      if (!Source.Contains(item)) {
        Source.Add(item);
        itemAdded = true;

        // if the group is not grouped schedule it for ReWrap
        if (!isGrouping) {
          toReWrap.Add(this);
          return;
        }
      }

      // done if the group is not grouped and the item was already in the source
      if (!isGrouping) return;

      // find existing group for the item and remove the item from other groups
      var groupFound = false;
      var emptyGroup = Items
        .OfType<CollectionViewGroup<T>>()
        .SingleOrDefault(x => x.GroupedBy == null);

      foreach (var group in Items.OfType<CollectionViewGroup<T>>().ToArray())
        if (group.GroupedBy?.ItemGroupBy(item, group.GroupedBy.Parameter) == true) {
          group.ReGroupItem(item, toReWrap);
          groupFound = true;
        }
        else if (!itemAdded && !ReferenceEquals(group, emptyGroup))
          group.RemoveItem(item, toReWrap);

      // add/remove the item in/from the empty group
      if (emptyGroup == null) return;
      if (groupFound)
        emptyGroup.RemoveItem(item, toReWrap);
      else if (!emptyGroup.Source.Contains(item)) {
        emptyGroup.Source.Add(item);
        toReWrap.Add(emptyGroup);
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

      row ??= AddRow();

      var usedSpace = row.Items.Sum(x => View.GetItemWidth(x));
      var itemWidth = View.GetItemWidth(item);

      if (Width - usedSpace < itemWidth)
        row = AddRow();

      row.Items.Add(item);
    }

    private CollectionViewRow<T> AddRow() {
      var row = new CollectionViewRow<T>(this);
      try {
        Items.Add(row);
      }
      catch (Exception) {
        // BUG in .NET remove try/catch after update to new .NET version
      }
      return row;
    }

    public void ExpandAll() {
      IsExpanded = true;
      if (Items.Count == 0) return;
      foreach (var item in Items.OfType<CollectionViewGroup<T>>())
        item.ExpandAll();
    }

    public static bool IsFullyExpanded(CollectionViewGroup<T> group) =>
      group.IsExpanded && (group.Parent == null || IsFullyExpanded(group.Parent));
  }
}
