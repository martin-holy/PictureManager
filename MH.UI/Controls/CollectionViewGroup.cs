using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MH.UI.Controls {
  public enum GroupMode {
    GroupBy,
    GroupByRecursive,
    ThenBy,
    ThenByRecursive
  }

  public class CollectionViewGroup<T> : ObservableObject where T : ISelectable {
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
    public bool IsRoot { get; set; }
    public bool IsRecursive { get; set; }
    public bool IsGroupBy { get; set; }
    public bool IsThenBy { get; set; }
    public bool IsExpanded { get => _isExpanded; set => SetIsExpanded(value); }
    public bool IsReWrapPending { get; set; } = true;

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
      Width = Parent.Width - View.GroupContentOffset;

      if (IsRoot || Parent.GroupByItems == null || !IsThenBy) return;

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

    public static void GroupIt(CollectionViewGroup<T> parent) {
      var groupByItems = GetGroupByItems(parent);
      if (groupByItems == null) return;

      CollectionViewGroup<T> emptyGroup = null;
      var newGroups = groupByItems
        .Select(x => new object[] { x, null })
        .ToArray();

      parent.Items.Clear();

      foreach (var item in parent.Source) {
        var fit = false;

        foreach (var grp in newGroups) {
          var gbi = (CollectionViewGroupByItem<T>)grp[0];
          if (!gbi.ItemGroupBy(item, gbi.Parameter)) continue;
          grp[1] ??= new CollectionViewGroup<T>(parent, gbi, new());
          ((CollectionViewGroup<T>)grp[1]).Source.Add(item);
          fit = true;
        }

        if (fit) continue;
        emptyGroup ??= new(parent, null, new());
        emptyGroup.Source.Add(item);
      }

      if (emptyGroup != null) {
        GroupIt(emptyGroup);
        parent.Items.Add(emptyGroup);
      }

      foreach (var newGroup in newGroups.Where(x => x[1] != null).Select(x => (CollectionViewGroup<T>)x[1])) {
        GroupIt(newGroup);
        parent.Items.Add(newGroup);
      }
    }

    public static CollectionViewGroupByItem<T>[] GetGroupByItems(CollectionViewGroup<T> group) {
      CollectionViewGroupByItem<T>[] groupByItems = null;

      if (group.GroupByItems != null && (group.IsRoot || group.GroupedBy is null or { Items: { Count: 0 } })) {
        if (group.IsGroupBy)
          groupByItems = group.GroupByItems.ToArray();
        else if (group.IsThenBy && group.GroupByItems.Length > 0)
          groupByItems = new[] { group.GroupByItems[0] };
      }
      else if (group.IsRecursive && group.GroupedBy?.Items?.Count > 0)
        groupByItems = group.GroupedBy.Items.Cast<CollectionViewGroupByItem<T>>().ToArray();

      return groupByItems;
    }

    public static void RemoveEmptyGroups(CollectionViewGroup<T> group, ISet<CollectionViewGroup<T>> toReWrap, List<CollectionViewGroup<T>> removedGroups) {
      var groups = group.Items.OfType<CollectionViewGroup<T>>().ToArray();

      if (groups.Length > 0) {
        foreach (var subGroup in groups)
          RemoveEmptyGroups(subGroup, toReWrap, removedGroups);

        return;
      }

      var removed = false;
      while (true) {
        if (group == null) break;

        if (group.Source.Count == 0
            || (group.GroupedBy is { IsGroup: true }
                && group.Items.Count == 0)
            || (group.GroupedBy == null
                && group.Parent?.Items.Count == 1
                && !group.Items.OfType<CollectionViewGroup<T>>().Any())) {
          group.Parent?.Items.Remove(group);
          removedGroups?.Add(group);
          removed = true;
        }
        else if (removed)
          toReWrap?.Add(group);

        group = group.Parent;
      }
    }

    public void InsertItem(T item, ISet<CollectionViewGroup<T>> toReWrap) {
      var groupByItems = GetGroupByItems(this);

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

      var first = Items.FirstOrDefault();
      if (first == null
          || first is CollectionViewRow<T>
          || (first is CollectionViewGroup<T> { GroupedBy: not null } fg
              && GroupedBy != null
              && !ReferenceEquals(fg.GroupedBy, GroupedBy))) {
        GroupIt(this);
        SetExpanded(true);
        return;
      }

      var groups = Items.OfType<CollectionViewGroup<T>>().ToArray();
      var inGroups = new List<CollectionViewGroup<T>>();

      foreach (var gbi in groupByItems) {
        if (!gbi.ItemGroupBy(item, gbi.Parameter)) continue;

        var group = groups.SingleOrDefault(x => ReferenceEquals(x.GroupedBy?.Parameter, gbi.Parameter));

        if (group != null)
          group.InsertItem(item, toReWrap);
        else {
          group = new(this, gbi, new() { item });
          GroupIt(group);
          group.SetExpanded(true);
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

    public void RemoveItem(T item, ISet<CollectionViewGroup<T>> toReWrap) {
      if (!Source.Remove(item)) return;

      if (Source.Count == 0) {
        Parent?.Items.Remove(this);
        return;
      }

      OnPropertyChanged(nameof(SourceCount));

      if (Items.FirstOrDefault() is CollectionViewRow<T>)
        toReWrap.Add(this);
      else
        foreach (var group in Items.OfType<CollectionViewGroup<T>>().ToArray())
          group.RemoveItem(item, toReWrap);
    }

    private void SetWidth(double width) {
      if (Math.Abs(Width - width) < 1) return;
      _width = width;
      ReWrap();

      foreach (var group in Items.OfType<CollectionViewGroup<T>>())
        group.Width = width - View.GroupContentOffset;
    }

    public static void ReWrapAll(CollectionViewGroup<T> group) {
      var groups = group.Items.OfType<CollectionViewGroup<T>>().ToArray();

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
          Items.Add(new CollectionViewRow<T>(this));

        return;
      }

      var newRows = WrapSource().ToArray();

      // add or remove rows to match the source
      if (Items.Count > newRows.Length) {
        Items.Execute(items => {
          while (items.Count > newRows.Length)
            items.RemoveAt(items.Count - 1);
        }, NotifyCollectionChangedAction.Remove);
      }
      else if (Items.Count < newRows.Length) {
        Items.Execute(items => {
          while (items.Count < newRows.Length)
            AddRow(items);
        }, NotifyCollectionChangedAction.Add);
      }

      for (int i = 0; i < newRows.Length; i++) {
        var oldRow = (CollectionViewRow<T>)Items[i];
        var newRow = newRows[i];

        if (oldRow.Items.SequenceEqual(newRow))
          continue;

        oldRow.Items.Execute(items => {
          items.Clear();
          foreach (var item in newRow)
            items.Add(item);
        });
      }
    }

    private IEnumerable<IList<T>> WrapSource() {
      var index = 0;
      var usedSpace = 0;

      for (int i = 0; i < Source.Count; i++) {
        var item = Source[i];
        var itemWidth = View.GetItemWidth(item);

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

      foreach (var g in parent.Items.OfType<CollectionViewGroup<T>>())
        if (FindItem(g, item, ref group, ref row))
          return true;

      group = parent;
      row = parent.Items
        .OfType<CollectionViewRow<T>>()
        .FirstOrDefault(x => x.Items.Contains(item));

      return true;
    }

    private void AddRow(ICollection<object> items) {
      var row = new CollectionViewRow<T>(this);
      try {
        items.Add(row);
      }
      catch (Exception) {
        // BUG in .NET remove try/catch after update to new .NET version
      }
    }

    private void SetIsExpanded(bool value) {
      _isExpanded = value;

      if (_isExpanded && IsReWrapPending) {
        ReWrap();
        IsReWrapPending = false;
      }

      OnPropertyChanged(nameof(IsExpanded));
    }

    public void SetExpanded(bool value) {
      if (IsExpanded != value)
        IsExpanded = value;
      if (Items.Count == 0) return;
      foreach (var group in Items.OfType<CollectionViewGroup<T>>())
        group.SetExpanded(value);
    }

    public void Clear() {
      Items.Clear();
      Source.Clear();
      OnPropertyChanged(nameof(SourceCount));
    }

    public static bool IsFullyExpanded(CollectionViewGroup<T> group) =>
      group.IsExpanded && (group.Parent == null || IsFullyExpanded(group.Parent));
  }
}
