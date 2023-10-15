using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls;

public enum GroupMode {
  GroupBy,
  GroupByRecursive,
  ThenBy,
  ThenByRecursive
}

public class CollectionViewGroup<T> : TreeItem, ICollectionViewGroup where T : ISelectable {
  private double _width;

  public CollectionView<T> View { get; set; }
  public List<T> Source { get; }
  public int SourceCount => Source.Count;
  public IEnumerable<CollectionViewGroup<T>> Groups => Items.OfType<CollectionViewGroup<T>>();
  public CollectionViewGroupByItem<T>[] GroupByItems { get; set; }
  public CollectionViewGroupByItem<T> GroupedBy { get; set; }
  public double Width { get => _width; set => SetWidth(value); }
  public bool IsGroupingRoot { get; set; }
  public bool IsRecursive { get; set; }
  public bool IsGroupBy { get; set; }
  public bool IsThenBy { get; set; }
  public bool IsReWrapPending { get; set; } = true;

  public CollectionViewGroup(List<T> source) {
    Source = source;
    OnPropertyChanged(nameof(SourceCount));
  }

  public CollectionViewGroup(CollectionViewGroup<T> parent, CollectionViewGroupByItem<T> groupedBy, List<T> source) : this (source) {
    GroupedBy = groupedBy;
    Parent = parent;
    View = parent.View;
    IsRecursive = parent.IsRecursive;
    IsGroupBy = parent.IsGroupBy;
    IsThenBy = parent.IsThenBy;
    Width = parent.Width - View.GroupContentOffset;
    GroupByItems = parent.GetGroupByItemsForSubGroup();
  }

  private CollectionViewGroupByItem<T>[] GetGroupByItemsForSubGroup() {
    if (GroupByItems == null || !IsThenBy)
      return null;
    if (IsRecursive && !IsGroupingRoot && GroupedBy?.Items?.Count > 0)
      return GroupByItems.ToArray();
    if (GroupByItems.Length > 1)
      return GroupByItems[1..];

    return null;
  }

  private CollectionViewGroupByItem<T>[] GetGroupByItemsForGrouping() {
    CollectionViewGroupByItem<T>[] groupByItems = null;

    if (GroupByItems != null && (IsGroupingRoot || GroupedBy is null or { Items: { Count: 0 } })) {
      if (IsGroupBy)
        groupByItems = GroupByItems.ToArray();
      else if (IsThenBy && GroupByItems.Length > 0)
        groupByItems = new[] { GroupByItems[0] };
    }
    else if (IsRecursive && !IsGroupingRoot && GroupedBy?.Items?.Count > 0)
      groupByItems = GroupedBy.Items.Cast<CollectionViewGroupByItem<T>>().ToArray();

    return groupByItems;
  }

  public void GroupIt() {
    Items.Clear();
    var groupByItems = GetGroupByItemsForGrouping();
    if (groupByItems == null) return;

    // first item reserved for empty group
    var newGroups = new CollectionViewGroup<T>[groupByItems.Length + 1];

    foreach (var item in Source) {
      var fit = false;

      for (int i = 0; i < groupByItems.Length; i++) {
        if (!groupByItems[i].Fit(item)) continue;
        newGroups[i + 1] ??= new(this, groupByItems[i], new());
        newGroups[i + 1].Source.Add(item);
        fit = true;
      }

      if (fit) continue;
      newGroups[0] ??= new(this, null, new());
      newGroups[0].Source.Add(item);
    }

    foreach (var newGroup in newGroups) {
      if (newGroup == null) continue;
      newGroup.GroupIt();
      Items.Add(newGroup);
    }
  }

  public static void RemoveEmptyGroups(CollectionViewGroup<T> group, ISet<CollectionViewGroup<T>> toReWrap, List<CollectionViewGroup<T>> removedGroups) {
    // go down the tree first
    var subGroups = group.Groups.ToArray();

    if (subGroups.Length > 0) {
      foreach (var subGroup in subGroups)
        RemoveEmptyGroups(subGroup, toReWrap, removedGroups);

      return;
    }

    // and then up the tree and check if is group empty
    var removed = false;
    while (true) {
      if (group == null) break;

      if (group.IsEmpty()) {
        group.Parent?.Items.Remove(group);
        removedGroups?.Add(group);
        removed = true;
      }
      else if (removed)
        toReWrap?.Add(group);

      group = group.Parent as CollectionViewGroup<T>;
    }
  }

  private bool IsEmpty() =>
    Source.Count == 0 // empty source
    || (!Groups.Any() // no sub groups
        && (GroupedBy is { IsGroup: true } // type group
            || (GroupedBy == null && Parent?.Items.Count == 1))); // only one "empty group"

  public void UpdateGroupByItems(CollectionViewGroupByItem<T>[] newGroupByItems) {
    if (GroupByItems == null) return;

    foreach (var gbi in GroupByItems)
      gbi.Update(newGroupByItems);
  }

  public void InsertItem(T item, ISet<CollectionViewGroup<T>> toReWrap) {
    var groupByItems = GetGroupByItemsForGrouping();

    // add the item to the source if is not present
    if (!Source.Contains(item)) {
      Source.AddInOrder(item, View.SortCompare);
      OnPropertyChanged(nameof(SourceCount));

      // if the group is not grouped schedule it for ReWrap
      if (groupByItems == null) {
        toReWrap.Add(this);
        return;
      }
    }

    // done if the group is not grouped and the item was already in the source
    if (groupByItems == null) return;

    // if the first not "empty" Group is not GroupedBy anything from groupByItems
    // in case when groups between this group and first group where removed as "empty"
    var firstNotEmpty = Items.FirstOrDefault(x => x is CollectionViewGroup<T> { GroupedBy: not null }) as CollectionViewGroup<T>;
    if (!groupByItems.Any(x => ReferenceEquals(x, firstNotEmpty?.GroupedBy))) {
      GroupIt();
      return;
    }

    var groups = Groups.ToArray();
    var inGroups = new List<CollectionViewGroup<T>>();

    // insert Item to existing group or create new one
    foreach (var gbi in groupByItems) {
      if (!gbi.Fit(item)) continue;

      var group = groups.SingleOrDefault(x => ReferenceEquals(x.GroupedBy?.Data, gbi.Data));

      if (group != null)
        group.InsertItem(item, toReWrap);
      else {
        group = new(this, gbi, new() { item });
        group.GroupIt();
        group.SetExpanded<CollectionViewGroup<T>>(true);
        Items.SetInOrder(group,
          x => x is CollectionViewGroup<T> { GroupedBy.Data: IListItem gn }
            ? gn.Name
            : string.Empty);
      }

      inGroups.Add(group);
    }

    // if Item did not fit in to any group => insert it to "empty" Group
    if (inGroups.Count == 0) {
      var emptyGroup = groups.SingleOrDefault(x => x.GroupedBy == null);

      if (emptyGroup == null) {
        emptyGroup = new(this, null, new()) { IsExpanded = true };
        Items.Insert(0, emptyGroup);
      }

      emptyGroup.InsertItem(item, toReWrap);
      inGroups.Add(emptyGroup);
    }

    // remove the Item from groups to which it did not fit
    foreach (var group in groups.Except(inGroups))
      group.RemoveItem(item, toReWrap);
  }

  public void RemoveItem(T item, ISet<CollectionViewGroup<T>> toReWrap) {
    if (!Source.Remove(item)) return;

    if (Source.Count == 0) {
      Parent?.Items.Remove(this);
      Clear();
      return;
    }

    OnPropertyChanged(nameof(SourceCount));

    if (Items.FirstOrDefault() is CollectionViewRow<T>)
      toReWrap.Add(this);
    else
      foreach (var group in Groups.ToArray())
        group.RemoveItem(item, toReWrap);
  }

  private void SetWidth(double width) {
    if (Math.Abs(Width - width) < 1) return;
    _width = width;
    OnPropertyChanged(nameof(Width));
    ReWrap();

    foreach (var group in Groups)
      group.Width = width - View.GroupContentOffset;
  }

  public static void ReWrapAll(CollectionViewGroup<T> group) {
    var groups = group.Groups.ToArray();

    if (groups.Length == 0)
      group.ReWrap();
    else
      foreach (var subGroup in groups)
        ReWrapAll(subGroup);
  }

  public void ReWrap() {
    if (Items.FirstOrDefault() is CollectionViewGroup<T> || !(Width > 0)) return;

    if (!IsExpanded) {
      IsReWrapPending = true;
      // placeholder for expander
      if (Items.Count == 0)
        Items.Add(new CollectionViewRow<T> { Parent = this });

      return;
    }

    var newRows = WrapSource().ToArray();

    // add or remove rows to match the source
    if (Items.Count > newRows.Length) {
      Items.Execute(items => {
        while (items.Count > newRows.Length)
          items.RemoveAt(items.Count - 1);
      });
    }
    else if (Items.Count < newRows.Length) {
      Items.Execute(items => {
        while (items.Count < newRows.Length)
          items.Add(new CollectionViewRow<T> { Parent = this });
      });
    }

    // update items in rows if necessary
    for (int i = 0; i < newRows.Length; i++) {
      var oldRow = (CollectionViewRow<T>)Items[i];
      var newRow = newRows[i];

      if (oldRow.Leaves.SequenceEqual(newRow))
        continue;

      oldRow.Leaves.Execute(items => {
        items.Clear();
        foreach (var item in newRow)
          items.Add(item);
      });
    }
  }

  public int GetItemSize(object item, bool getWidth) =>
    View.GetItemSize((T)item, getWidth);

  private IEnumerable<IList<T>> WrapSource() {
    var index = 0;
    var usedSpace = 0;

    for (int i = 0; i < Source.Count; i++) {
      var item = Source[i];
      var itemWidth = GetItemSize(item, true);

      if (Width - usedSpace < itemWidth) {
        yield return Source.GetRange(index, i - index);
        index = i;
        usedSpace = 0;
      }

      usedSpace += itemWidth;
    }

    yield return Source.GetRange(index, Source.Count - index);
  }

  public static bool FindItem(CollectionViewGroup<T> parent, T item, ref CollectionViewGroup<T> group, ref CollectionViewRow<T> row) {
    if (!parent.Source.Contains(item)) return false;
    parent.IsExpanded = true;

    foreach (var g in parent.Groups)
      if (FindItem(g, item, ref group, ref row))
        return true;

    group = parent;
    row = parent.Items
      .OfType<CollectionViewRow<T>>()
      .FirstOrDefault(x => x.Leaves.Contains(item));

    return true;
  }

  public override void OnIsExpandedChanged(bool value) {
    if (!value || !IsReWrapPending) return;
    ReWrap();
    IsReWrapPending = false;
  }

  public void Clear() {
    Items.Clear();
    Source.Clear();
    OnPropertyChanged(nameof(SourceCount));
  }
}