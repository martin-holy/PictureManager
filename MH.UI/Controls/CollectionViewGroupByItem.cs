using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls {
  public class CollectionViewGroupByItem<T> : TreeItem<CollectionViewGroupByItem<T>, CollectionViewGroupByItem<T>> {
    public Func<T, object, bool> ItemGroupBy { get; }
    public bool IsGroup { get; set; }

    public CollectionViewGroupByItem(ITitled data, Func<T, object, bool> itemGroupBy) : base(null, data) {
      ItemGroupBy = itemGroupBy;
    }

    public static List<CollectionViewGroupByItem<TItem>> BuildTree<TItem, TGroup, TSort>(
      IEnumerable<TGroup> source,
      Func<TGroup, CollectionViewGroupByItem<TItem>> getGroupByItem,
      Func<TGroup, TSort> orderBy) where TGroup : ITreeItem {

      var root = new List<CollectionViewGroupByItem<TItem>>();
      var all = source
        .SelectMany(x => x.GetThisAndParents<TGroup>())
        .Distinct()
        .ToDictionary(x => x, getGroupByItem);

      foreach (var item in all.OrderBy(x => orderBy(x.Key))) {
        if (item.Key.Parent is not TGroup parent) {
          root.Add(item.Value);
          continue;
        }

        item.Value.Parent = all[parent];
        item.Value.Parent.Items.Add(item.Value);
      }

      return root;
    }

    // TODO remove items as well
    public void Update(CollectionViewGroupByItem<T>[] items) {
      var newItems = FindItem(items, x => ReferenceEquals(x.Data, Data))?.Items.ToArray();
      if (newItems == null) return;
      var itemItems = Items.ToArray();

      foreach (var itemItem in itemItems)
        itemItem.Update(items);

      foreach (var newItem in newItems) {
        if (itemItems.Any(x => ReferenceEquals(x.Data, newItem.Data)))
          continue;

        Items.SetInOrder(newItem, x => x.Data is ITitled d ? d.GetTitle : string.Empty);
      }
    }
  }
}
