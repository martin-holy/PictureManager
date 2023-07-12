using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls {
  public enum GroupMode {
    GroupBy,
    GroupByRecursive,
    ThenBy,
    ThenByRecursive
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
    public double Width { get => _width; set => SetWidth(value); }
    public string Icon { get; set; }
    public string Title { get; set; }
    public bool IsRecursive { get; set; }
    public bool IsGroupBy { get; set; }
    public bool IsThenBy { get; set; }
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
      IsRecursive = Parent.IsRecursive;
      IsGroupBy = Parent.IsGroupBy;
      IsThenBy = Parent.IsThenBy;

      if (Parent.GroupByItems == null || !IsThenBy) return;

      if (IsRecursive && Parent.GroupedBy?.Items?.Count > 0)
        GroupByItems = Parent.GroupByItems.ToArray();
      else if (Parent.GroupByItems.Length > 1)
        GroupByItems = Parent.GroupByItems[1..];
    }

    public CollectionViewGroup(List<T> source, string icon, string title, CollectionView<T> view, GroupMode groupMode, CollectionViewGroupByItem<T>[] groupByItems) : this(null, null, source) {
      Icon = icon;
      Title = title;
      View = view;
      IsGroupBy = groupMode is GroupMode.GroupBy or GroupMode.GroupByRecursive;
      IsThenBy = groupMode is GroupMode.ThenBy or GroupMode.ThenByRecursive;
      IsRecursive = groupMode is GroupMode.GroupByRecursive or GroupMode.ThenByRecursive;
      GroupByItems = groupByItems?.Length == 0 ? null : groupByItems;
    }

    public void GroupIt() {
      if (IsGroupBy)
        CreateGroups(this, GroupByItems);
      else if (IsThenBy) 
        GroupByThenBy();
    }

    private static CollectionViewGroup<T> CreateGroup(CollectionViewGroup<T> parent, CollectionViewGroupByItem<T> groupBy, ICollection<T> notInGroup) {
      var source = new List<T>();

      foreach (var item in parent.Source.Where(x => groupBy.ItemGroupBy(x, groupBy.Parameter))) {
        source.Add(item);
        notInGroup?.Remove(item);
      }

      return source.Count == 0
        ? null
        : new(parent, groupBy, source);
    }

    private static void CreateGroups(CollectionViewGroup<T> parent, IEnumerable<CollectionViewGroupByItem<T>> groupBys) {
      if (parent == null || groupBys == null) return;
      var notInGroups = parent.Source.ToList();
      parent.Items.Clear();

      foreach (var gbi in groupBys) {
        var group = CreateGroup(parent, gbi, notInGroups);
        if (group == null) continue;
        if (gbi.IsGroup) group.IsExpanded = true;
        if (parent.IsRecursive && gbi.Items?.Count > 0)
          CreateGroups(group, gbi.Items.Cast<CollectionViewGroupByItem<T>>());

        parent.Items.Add(group);
      }

      if (parent.Items.Count == 1
          && parent.Items[0] is CollectionViewGroup<T> { GroupedBy: { IsGroup: true } } g
          && g.Items.Count == 0) {
        parent.Items.Clear();
        parent.ReWrap();
      }

      if (notInGroups.Count == 0 || parent.Items.Count == 0) return;
      notInGroups.TrimExcess();
      parent.Items.Insert(0, new CollectionViewGroup<T>(parent, null, notInGroups));
    }

    private void GroupByThenBy() {
      if (GroupByItems == null) return;

      CreateGroups(this, new[] { GroupByItems[0] });

      foreach (var group in Items.OfType<CollectionViewGroup<T>>()) {
        if (IsRecursive) {
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

    public void InsertItem(T item, ISet<CollectionViewGroup<T>> toReWrap) {
      var isGrouping = GroupByItems != null || (IsRecursive && GroupedBy?.Items?.Count > 0);

      // add the item to the source if is not present
      if (!Source.Contains(item)) {
        Source.AddInOrder(item, (a, b) => string.Compare(View.ItemOrderBy(a), View.ItemOrderBy(b), StringComparison.CurrentCultureIgnoreCase) > 0);
        OnPropertyChanged(nameof(SourceCount));

        // if the group is not grouped schedule it for ReWrap
        if (!isGrouping) {
          toReWrap.Add(this);
          return;
        }
      }

      // done if the group is not grouped and the item was already in the source
      if (!isGrouping) return;

      if (GroupByItems != null && Items.FirstOrDefault() is null or CollectionViewRow<T>) {
        GroupIt();
        ExpandAll();
        return;
      }

      CollectionViewGroupByItem<T>[] groupByItems = null;

      if (GroupByItems != null && GroupedBy is null or { Items: { Count: 0 } }) {
        if (IsGroupBy)
          groupByItems = GroupByItems.ToArray();
        else if (GroupByItems.Length > 0)
          groupByItems = new[] { GroupByItems[0] };
      }
      else if (IsRecursive && GroupedBy?.Items?.Count > 0)
        groupByItems = GroupedBy.Items.Cast<CollectionViewGroupByItem<T>>().ToArray();

      if (groupByItems == null || groupByItems.All(x => x.IsGroup && x.Items?.Count == 0)) return;

      var groups = Items.OfType<CollectionViewGroup<T>>().ToArray();
      var inGroups = new List<CollectionViewGroup<T>>();

      foreach (var gbi in groupByItems) {
        if (!gbi.ItemGroupBy(item, gbi.Parameter)) continue;

        var group = groups.SingleOrDefault(x => ReferenceEquals(x.GroupedBy?.Parameter, gbi.Parameter));

        if (group != null)
          group.InsertItem(item, toReWrap);
        else {
          group = new(this, gbi, new() { item });
          group.GroupIt();
          group.ExpandAll();
          Items.SetInOrder(group, x => x is CollectionViewGroup<T> g ? g.Title : string.Empty);
        }

        inGroups.Add(group);
      }

      if (inGroups.Count == 0) {
        var emptyGroup = groups.SingleOrDefault(x => x.GroupedBy == null);

        if (emptyGroup == null) {
          emptyGroup = new(this, null, new()) { IsExpanded = true };
          Items.Insert(0, emptyGroup);
        }

          emptyGroup.InsertItem(item, toReWrap);
        inGroups.Add(emptyGroup);
      }

      foreach (var group in groups.Except(inGroups))
        group.RemoveItem(item, toReWrap);
    }

    public void UpdateGroupByItems(CollectionViewGroupByItem<T>[] newGroupByItems) {
      if (GroupByItems == null) return;

      foreach (var gbi in GroupByItems)
        gbi.Update(newGroupByItems);
    }

    private static bool RemoveGroupIfEmpty(CollectionViewGroup<T> group, ISet<CollectionViewGroup<T>> toReWrap) {
      var removed = false;
      while (true) {
        if (group == null) break;

        // the group have 0 items or
        // the group have only one sub group of type (all from source except what fit to siblings) or
        // the group have only one empty sub group of type (group of CollectionViewGroupByItem)
        if (group.Source.Count == 0
            || (group.Items.Count == 1 && group.Items[0] is CollectionViewGroup<T> gr
              && (gr.GroupedBy == null || gr.GroupedBy is { IsGroup: true } g && g.Items.Count == 0))) {
          group.Parent?.Items.Remove(group);
          removed = true;
        }
        else if (removed)
          toReWrap?.Add(group);

        group = group.Parent;
      }

      return removed;
    }

    public void RemoveItem(T item, ISet<CollectionViewGroup<T>> toReWrap) {
      if (!Source.Remove(item)) return;
      if (RemoveGroupIfEmpty(this, toReWrap)) return;

      if (Source.Count == 0) {
        Parent?.Items.Remove(this);
        Items.Clear();
        return;
      }

      OnPropertyChanged(nameof(SourceCount));

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
