using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls {
  public class CollectionViewGroupByItem<T> : TreeItem {
    public object Parameter { get; }
    public Func<T, object, bool> ItemGroupBy { get; }
    public bool IsGroup { get; set; }

    public CollectionViewGroupByItem(string icon, string title, object parameter, Func<T, object, bool> itemGroupBy) {
      IconName = icon;
      Name = title;
      Parameter = parameter;
      ItemGroupBy = itemGroupBy;
    }

    public static List<CollectionViewGroupByItem<TItem>> BuildTree<TItem, TGroup, TSort>(
      IEnumerable<TGroup> source,
      Func<TGroup, CollectionViewGroupByItem<TItem>> getGroupByItem,
      Func<TGroup, TSort> orderBy) where TGroup : ITreeItem {

      var root = new List<CollectionViewGroupByItem<TItem>>();
      var all = source
        .SelectMany(x => x.GetThisAndParentRecursive())
        .Distinct()
        .ToDictionary(x => x, getGroupByItem);

      foreach (var item in all.OrderBy(x => orderBy(x.Key))) {
        if (item.Key.Parent is not TGroup parent) {
          root.Add(item.Value);
          continue;
        }

        all[parent].AddItem(item.Value);
      }

      return root;
    }

    public void Update(CollectionViewGroupByItem<T>[] items) {
      var newItems = Tree.FindChild<CollectionViewGroupByItem<T>>(
          items, x => ReferenceEquals(x.Parameter, Parameter))?.Items
        .Cast<CollectionViewGroupByItem<T>>()
        .ToArray();

      if (newItems == null) return;

      var itemItems = Items.Cast<CollectionViewGroupByItem<T>>().ToArray();

      foreach (var itemItem in itemItems)
        itemItem.Update(items);

      foreach (var newItem in newItems) {
        if (itemItems.Any(x => ReferenceEquals(x.Parameter, newItem.Parameter)))
          continue;

        Items.SetInOrder(newItem, x => x.Name);
      }
    }
  }
}
