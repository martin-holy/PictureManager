using MH.UI.Interfaces;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
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

  public class CollectionViewGroup<T> : TreeItem, ICollectionViewGroup<T> where T : ISelectable {
    private double _width;

    public CollectionView<T> View { get; set; }
    public List<T> Source { get; }
    public int SourceCount => Source.Count;
    public IEnumerable<ICollectionViewGroup<T>> Groups => Items.OfType<ICollectionViewGroup<T>>();
    public CollectionViewGroupByItem<T>[] GroupByItems { get; set; }
    public CollectionViewGroupByItem<T> GroupedBy { get; set; }
    public double Width { get => _width; set => SetWidth(value); }
    public bool IsRoot { get; set; }
    public bool IsRecursive { get; set; }
    public bool IsGroupBy { get; set; }
    public bool IsThenBy { get; set; }
    public bool IsReWrapPending { get; set; } = true;

    public CollectionViewGroup(ICollectionViewGroup<T> parent, CollectionViewGroupByItem<T> groupedBy, List<T> source) {
      GroupedBy = groupedBy;
      Source = source;

      OnPropertyChanged(nameof(SourceCount));

      if (parent == null) return;

      Parent = parent;
      View = parent.View;
      IsRecursive = parent.IsRecursive;
      IsGroupBy = parent.IsGroupBy;
      IsThenBy = parent.IsThenBy;
      Width = parent.Width - View.GroupContentOffset;

      if (IsRoot || parent.GroupByItems == null || !IsThenBy) return;

      if (IsRecursive && parent.GroupedBy?.Items?.Count > 0)
        GroupByItems = parent.GroupByItems.ToArray();
      else if (parent.GroupByItems.Length > 1)
        GroupByItems = parent.GroupByItems[1..];
    }

    public CollectionViewGroup(List<T> source, CollectionView<T> view, GroupMode groupMode, CollectionViewGroupByItem<T>[] groupByItems) : this(default, null, source) {
      View = view;
      GroupedBy = new(new ListItem(view.Icon, view.Name, view), null);
      IsGroupBy = groupMode is GroupMode.GroupBy or GroupMode.GroupByRecursive;
      IsThenBy = groupMode is GroupMode.ThenBy or GroupMode.ThenByRecursive;
      IsRecursive = groupMode is GroupMode.GroupByRecursive or GroupMode.ThenByRecursive;
      GroupByItems = groupByItems?.Length == 0 ? null : groupByItems;
    }

    public static void GroupIt(ICollectionViewGroup<T> parent) {
      var groupByItems = GetGroupByItems(parent);
      if (groupByItems == null) return;

      ICollectionViewGroup<T> emptyGroup = null;
      var newGroups = groupByItems
        .Select(x => new object[] { x, null })
        .ToArray();

      parent.Items.Clear();

      foreach (var item in parent.Source) {
        var fit = false;

        foreach (var grp in newGroups) {
          var gbi = (CollectionViewGroupByItem<T>)grp[0];
          if (!gbi.ItemGroupBy(item, gbi.Data)) continue;
          grp[1] ??= new CollectionViewGroup<T>(parent, gbi, new());
          ((ICollectionViewGroup<T>)grp[1]).Source.Add(item);
          fit = true;
        }

        if (fit) continue;
        emptyGroup ??= new CollectionViewGroup<T>(parent, null, new());
        emptyGroup.Source.Add(item);
      }

      if (emptyGroup != null) {
        GroupIt(emptyGroup);
        parent.Items.Add(emptyGroup);
      }

      foreach (var newGroup in newGroups.Where(x => x[1] != null).Select(x => (ICollectionViewGroup<T>)x[1])) {
        GroupIt(newGroup);
        parent.Items.Add(newGroup);
      }
    }

    public static CollectionViewGroupByItem<T>[] GetGroupByItems(ICollectionViewGroup<T> group) {
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

    public static void RemoveEmptyGroups(ICollectionViewGroup<T> group, ISet<ICollectionViewGroup<T>> toReWrap, List<ICollectionViewGroup<T>> removedGroups) {
      var groups = group.Groups.ToArray();

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
                && !group.Groups.Any())) {
          group.Parent?.Items.Remove(group);
          removedGroups?.Add(group);
          removed = true;
        }
        else if (removed)
          toReWrap?.Add(group);

        group = group.Parent as ICollectionViewGroup<T>;
      }
    }

    public void InsertItem(T item, ISet<ICollectionViewGroup<T>> toReWrap) {
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

      // TODO BUG in segments when there are no people and no keywords => set keyword and than person to one segment
      // this code works in case of the bug, but also recreates groups in other cases
      // when is not needed causing wrong scroll to alter because groups has ben recreated 
      /*var first = Items.FirstOrDefault();
      if (first == null
          || first is ICollectionViewRow<T>
          || (first is ICollectionViewGroup<T> { GroupedBy: not null } fg
              && GroupedBy != null
              && !ReferenceEquals(fg.GroupedBy, GroupedBy))) {
        GroupIt(this);
        SetExpanded<ICollectionViewGroup<T>>(true);
        return;
      }*/

      var first = Items.FirstOrDefault();
      if (first is null or ICollectionViewRow<T>) {
        GroupIt(this);
        this.SetExpanded<ICollectionViewGroup<T>>(true);
        return;
      }

      var groups = Groups.ToArray();
      var inGroups = new List<ICollectionViewGroup<T>>();

      foreach (var gbi in groupByItems) {
        if (!gbi.ItemGroupBy(item, gbi.Data)) continue;

        var group = groups.SingleOrDefault(x => ReferenceEquals(x.GroupedBy?.Data, gbi.Data));

        if (group != null)
          group.InsertItem(item, toReWrap);
        else {
          group = new CollectionViewGroup<T>(this, gbi, new() { item });
          GroupIt(group);
          group.SetExpanded<ICollectionViewGroup<T>>(true);
          Items.SetInOrder(group,
            x => x is ICollectionViewGroup<T> { GroupedBy: { Data: IListItem gn } }
              ? gn.Name
              : string.Empty);
        }

        inGroups.Add(group);
      }

      if (inGroups.Count == 0) {
        var emptyGroup = groups.SingleOrDefault(x => x.GroupedBy == null);

        if (emptyGroup == null) {
          emptyGroup = new CollectionViewGroup<T>(this, null, new()) { IsExpanded = true };
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

    public void RemoveItem(T item, ISet<ICollectionViewGroup<T>> toReWrap) {
      if (!Source.Remove(item)) return;

      if (Source.Count == 0) {
        Parent?.Items.Remove(this);
        return;
      }

      OnPropertyChanged(nameof(SourceCount));

      if (Items.FirstOrDefault() is ICollectionViewRow<T>)
        toReWrap.Add(this);
      else
        foreach (var group in Groups.ToArray())
          group.RemoveItem(item, toReWrap);
    }

    private void SetWidth(double width) {
      if (Math.Abs(Width - width) < 1) return;
      _width = width;
      ReWrap();

      foreach (var group in Groups)
        group.Width = width - View.GroupContentOffset;
    }

    public static void ReWrapAll(ICollectionViewGroup<T> group) {
      var groups = group.Groups.ToArray();

      if (groups.Length == 0)
        group.ReWrap();
      else
        foreach (var subGroup in groups)
          ReWrapAll(subGroup);
    }

    public void ReWrap() {
      if (Items.FirstOrDefault() is ICollectionViewGroup<T> || !(Width > 0)) return;

      if (!IsExpanded) {
        IsReWrapPending = true;
        // placeholder for expander
        if (Items.Count == 0) AddRow(Items);

        return;
      }

      var newRows = WrapSource().ToArray();

      // TODO ChangedAction
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
            AddRow(items);
        });
      }

      for (int i = 0; i < newRows.Length; i++) {
        var oldRow = (ICollectionViewRow<T>)Items[i];
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

    public static bool FindItem(ICollectionViewGroup<T> parent, T item, ref ICollectionViewGroup<T> group, ref ICollectionViewRow<T> row) {
      if (!parent.Source.Contains(item)) return false;
      parent.IsExpanded = true;

      foreach (var g in parent.Groups)
        if (FindItem(g, item, ref group, ref row))
          return true;

      group = parent;
      row = parent.Items
        .OfType<ICollectionViewRow<T>>()
        .FirstOrDefault(x => x.Leaves.Contains(item));

      return true;
    }

    private void AddRow(ICollection<ITreeItem> items) {
      var row = new CollectionViewRow<T> { Parent = this };
      try {
        items.Add(row);
      }
      catch (Exception) {
        // BUG in .NET remove try/catch after update to new .NET version
      }
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
}
