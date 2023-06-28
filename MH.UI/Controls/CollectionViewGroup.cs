using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System;
using System.Collections.Generic;
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
    public List<T> Source { get; }
    public int SourceCount => Source.Count;
    public ExtObservableCollection<object> Items { get; } = new();
    public CollectionViewGroupByItem<T>[] GroupByItems { get; set; }
    public CollectionViewGroupByItem<T> GroupedBy { get; set; }
    public GroupMode GroupMode { get; set; }
    public double Width { get => _width; set => SetWidth(value); }
    public string Icon { get; set; }
    public string Title { get; set; }
    // TODO lazy load OnExpanded
    public bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); } }

    public CollectionViewGroup(CollectionViewGroup<T> parent, CollectionViewGroupByItem<T> groupedBy, List<T> source) {
      Parent = parent;
      Icon = groupedBy == null ? string.Empty : groupedBy.IconName;
      Title = groupedBy == null ? string.Empty : groupedBy.Name;
      GroupedBy = groupedBy;
      Source = source;

      OnPropertyChanged(nameof(SourceCount));

      if (Parent == null) return;

      View = Parent.View;
      GroupMode = Parent.GroupMode;

      if (GroupedBy == null)
        Parent.Items.Insert(0, this);
      else
        Parent.Items.Add(this);

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
          CreateGroups(this, GroupByItems, false);
          break;
        case GroupMode.ThanBy or GroupMode.ThanByRecursive:
          GroupByThenBy();
          break;
      }
    }

    private static CollectionViewGroup<T> CreateGroup(CollectionViewGroup<T> parent, CollectionViewGroupByItem<T> groupBy, ICollection<T> notInGroup) {
      var source = new List<T>();

      foreach (var item in parent.Source.Where(x => groupBy.ItemGroupBy(x, groupBy.Parameter))) {
        source.Add(item);
        notInGroup.Remove(item);
      }

      return source.Count == 0
        ? null
        : new(parent, groupBy, source);
    }

    private static void CreateGroups(CollectionViewGroup<T> parent, IEnumerable<CollectionViewGroupByItem<T>> groupBys, bool patch) {
      if (parent == null || groupBys == null) return;
      var notInGroups = parent.Source.ToList();
      if (!patch) parent.Items.Clear();

      foreach (var gbi in groupBys) {
        if (patch && parent.Items
            .OfType<CollectionViewGroup<T>>()
            .Any(x => ReferenceEquals(x.GroupedBy, gbi)))
          continue;

        var group = CreateGroup(parent, gbi, notInGroups);
        if (group == null) continue;
        if (parent.GroupMode is GroupMode.GroupByRecursive or GroupMode.ThanByRecursive && gbi.Items?.Count > 0)
          CreateGroups(group, gbi.Items.Cast<CollectionViewGroupByItem<T>>(), false);
      }

      if (patch) {
        parent.Items.Sort(x => ((CollectionViewGroup<T>)x).Title);
        return;
      }

      // TODO don't do just one empty group. clear grouping in that case
      if (notInGroups.Count == 0) return;
      notInGroups.TrimExcess();
      var _ = new CollectionViewGroup<T>(parent, null, notInGroups);
    }

    private void GroupByThenBy() {
      if (GroupByItems == null) return;

      CreateGroups(this, new[] { GroupByItems[0] }, false);

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
      var toReWrap = new HashSet<CollectionViewGroup<T>>();

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

    private void ReGroupItem(T item, ISet<CollectionViewGroup<T>> toReWrap) {
      var itemAdded = false;
      var isGrouping = GroupByItems != null || GroupedBy?.Items?.Count > 0;

      // add the item to the source if is not present
      if (!Source.Contains(item)) {
        Source.AddInOrder(item, (a, b) => string.Compare(View.ItemOrderBy(a), View.ItemOrderBy(b), StringComparison.CurrentCultureIgnoreCase) > 0);
        OnPropertyChanged(nameof(SourceCount));
        itemAdded = true;

        // if the group is not grouped schedule it for ReWrap
        if (!isGrouping) {
          toReWrap.Add(this);
          return;
        }
      }

      // done if the group is not grouped and the item was already in the source
      if (!isGrouping) return;

      // find group for the item and remove the item from other groups
      var groupFound = false;
      CollectionViewGroup<T> emptyGroup = null;

      foreach (var group in Items.OfType<CollectionViewGroup<T>>().ToArray())
        if (group.GroupedBy == null)
          emptyGroup = group;
        else
          if (group.GroupedBy.ItemGroupBy(item, group.GroupedBy.Parameter)) {
            group.ReGroupItem(item, toReWrap);
            groupFound = true;
          }
          else if (!itemAdded)
            group.RemoveItem(item, toReWrap);

      // add/remove/patch the item
      if (groupFound)
        emptyGroup?.RemoveItem(item, toReWrap);
      else if (PatchGroups(this, item))
        emptyGroup?.RemoveItem(item, toReWrap);
      else
        emptyGroup?.ReGroupItem(item, toReWrap);
    }

    private bool PatchGroups(CollectionViewGroup<T> parent, T item) {
      var groupBy = View.GetGroupByItems(new[] { item }).ToArray();

      var commonGroupBys = parent.GroupByItems?
        .Where(x => groupBy.Any(y => ReferenceEquals(x.Parameter, y.Parameter)))
        .ToArray();

      if (commonGroupBys?.Length > 0) {
        CreateGroups(this, commonGroupBys, true);
        return true;
      }

      if (parent.GroupedBy?.Items?.FirstOrDefault()?.Parent is not CollectionViewGroupByItem<T> root)
        return false;

      var itemGroupBy = Tree.FindChild<CollectionViewGroupByItem<T>>(
        groupBy, x => ReferenceEquals(x.Parameter, root.Parameter));

      if (itemGroupBy == null) return false;

      // TODO DistinctBy when moving to new .NET version
      var groupBys = itemGroupBy.Items
        .Concat(root.Items)
        .Cast<CollectionViewGroupByItem<T>>()
        .GroupBy(x => x.Parameter)
        .Select(x => x.First());

      CreateGroups(parent, groupBys, true);

      return true;
    }

    private void RemoveItem(T item, ISet<CollectionViewGroup<T>> toReWrap) {
      if (!Source.Remove(item)) return;

      OnPropertyChanged(nameof(SourceCount));

      // remove the Group from its Parent if it is empty
      if (Source.Count == 0) {
        Parent?.Items.Remove(this);
        Items.Clear();
        return;
      }

      // schedule the Group for reWrap if doesn't have any subGroups
      if (Items.FirstOrDefault() is CollectionViewRow<T>) {
        toReWrap.Add(this);
        return;
      }

      // remove the Item from subGroups
      foreach (var group in Items.OfType<CollectionViewGroup<T>>().ToArray())
        group.RemoveItem(item, toReWrap);
    }

    private void SetWidth(double width) {
      if (Math.Abs(Width - width) < 1) return;
      _width = width;
      ReWrap();
    }

    public void ReWrap() {
      if (Items.FirstOrDefault() is CollectionViewGroup<T> || !(Width > 0)) return;

      Items.Execute(items => {
        items.Clear();

        foreach (var item in Source)
          AddItem(item, items);
      });
    }

    private void AddItem(T item, IList<object> items) {
      CollectionViewRow<T> row = null;

      if (items.Count > 0)
        row = items[^1] as CollectionViewRow<T>;

      row ??= AddRow(items);

      var usedSpace = row.Items.Sum(x => View.GetItemWidth(x));
      var itemWidth = View.GetItemWidth(item);

      if (Width - usedSpace < itemWidth)
        row = AddRow(items);

      row.Items.Add(item);
    }

    private CollectionViewRow<T> AddRow(ICollection<object> items) {
      var row = new CollectionViewRow<T>(this);
      try {
        items.Add(row);
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
