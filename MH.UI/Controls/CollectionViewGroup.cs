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
          CreateGroups(this, GroupByItems);
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
        if (parent.GroupMode is GroupMode.GroupByRecursive or GroupMode.ThanByRecursive && gbi.Items?.Count > 0)
          CreateGroups(group, gbi.Items.Cast<CollectionViewGroupByItem<T>>());

        parent.Items.Add(group);
      }

      // TODO use RemoveIfEmpty
      if (parent.Items.Count == 1
          && parent.Items[0] is CollectionViewGroup<T> { GroupedBy: { IsGroup: true } } g
          && g.Items.Count == 0) {
        parent.Items.Clear();
        parent.ReWrap();
      }
      //RemoveGroupIfEmpty(parent);

      if (notInGroups.Count == 0 || parent.Items.Count == 0) return;
      notInGroups.TrimExcess();
      parent.Items.Insert(0, new CollectionViewGroup<T>(parent, null, notInGroups));
    }

    private void GroupByThenBy() {
      if (GroupByItems == null) return;

      CreateGroups(this, new[] { GroupByItems[0] });

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

    public void InsertItem(T item, ISet<CollectionViewGroup<T>> toReWrap, CollectionViewGroupByItem<T>[] itemGroupBys) {
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
      var groups = Items.OfType<CollectionViewGroup<T>>().ToArray();
      var groupFound = false;
      CollectionViewGroup<T> emptyGroup = null;

      foreach (var group in groups)
        if (group.GroupedBy == null)
          emptyGroup = group;
        else if (group.GroupedBy.ItemGroupBy(item, group.GroupedBy.Parameter)) {
          group.InsertItem(item, toReWrap, itemGroupBys);
          groupFound = true;
        }
        else if (!itemAdded)
          group.RemoveItem(item, toReWrap);

      if (groupFound) {
        emptyGroup?.RemoveItem(item, toReWrap);
        return;
      }

      if (PatchGroups(this, item, toReWrap, itemGroupBys, emptyGroup)) {
        emptyGroup?.RemoveItem(item, toReWrap);
        return;
      }

      if (groups.Length == 0) {
        toReWrap.Add(this);
        return;
      }

      if (emptyGroup == null) {
        emptyGroup = new(this, null, new() { item });
        Items.Insert(0, emptyGroup);
      }
      else
        emptyGroup.InsertItem(item, toReWrap, itemGroupBys);
    }

    // ted to vypada dobre, ale je tam bug
    // po prirazeni persona, v nepersonoj priradit keyword a pak priradit persona a vznikne 
    // prazdna skupina, ktera by se mela odstranit, ale protoze tam je, tak dalsi prirazenej person
    // znemozni otevreni prazdne skupiny. mozna tam ma bejt rewrap

    // dalsi bug je, kdyz ma segment uz keyword a prida se mu dalsi, tak se ta dalsi groupa nevytvori

    // BUG sort groups or add in order
    private static bool PatchGroups(CollectionViewGroup<T> parent, T item, ISet<CollectionViewGroup<T>> toReWrap, CollectionViewGroupByItem<T>[] groupByItems, CollectionViewGroup<T> emptyGroup) {
      if (parent.GroupByItems != null && parent.Items.FirstOrDefault() is CollectionViewRow<T>) {
        parent.GroupIt();
        parent.ExpandAll();
        return true;
      }
      
      if (parent.GroupedBy?.Items?.Count > 0) {
        var fit = false;

        foreach (var gbi in parent.GroupedBy.Items.Cast<CollectionViewGroupByItem<T>>()) {
          if (gbi.ItemGroupBy(item, gbi.Parameter)) {
            fit = true;
            var group = CreateGroup(parent, gbi, null);
            if (group == null) continue;

            parent.Items.Sort(x => x is CollectionViewGroup<T> g ? g.Title : string.Empty);
            group.GroupIt();
            group.ExpandAll();
            parent.Items.Add(group);
          }
        }

        return fit;
      }
      
      return false;
    }

    public void UpdateGroupByItems(CollectionViewGroupByItem<T>[] newGroupByItems) {
      if (GroupByItems == null) return;

      foreach (var gbi in GroupByItems)
        gbi.Update(newGroupByItems);
    }

    private static bool RemoveGroupIfEmpty(CollectionViewGroup<T> group) {
      while (true) {
        if (group == null) break;

        if (group.Source.Count == 0) {
          group.Parent?.Items.Remove(group);
          group.Items.Clear();
          group.ReWrap();
          return true;
        }

        // the group have only one empty sub group of type (group of CollectionViewGroupByItem)
        if (group.Items.Count == 1 && group.Items[0] is CollectionViewGroup<T> { GroupedBy: { IsGroup: true } } g && g.Items.Count == 0) {
          // BUG tenhle radek tam nesmi bejt na vlozeni, ale musi bejt na odebrani
          group.Parent?.Items.Remove(group);
          group.Items.Clear();
          group.ReWrap();
          return true;
        }

        // the group have only one sub group of type (all from source except what fit to siblings)
        if (group.Items.Count == 1 && group.Items[0] is CollectionViewGroup<T> { GroupedBy: null }) {
          group.Parent?.Items.Remove(group);
          group.Items.Clear();
          group.ReWrap();
          return true;
        }

        group = group.Parent;
      }

      return false;
    }

    public void RemoveItem(T item, ISet<CollectionViewGroup<T>> toReWrap) {
      if (!Source.Remove(item)) return;
      if (RemoveGroupIfEmpty(this)) return;

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
