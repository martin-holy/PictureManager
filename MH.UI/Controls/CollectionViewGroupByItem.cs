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

    public static void Update(CollectionViewGroupByItem<T> item, CollectionViewGroupByItem<T>[] items) {
      var newGbi = Tree.FindChild<CollectionViewGroupByItem<T>>(
        items, x => ReferenceEquals(x.Parameter, item.Parameter));

      if (newGbi == null) return;

      foreach (var itemItem in item.Items.Cast<CollectionViewGroupByItem<T>>())
        Update(itemItem, items);

      foreach (var gbi in newGbi.Items.Except(item.Items).ToArray())
        item.Items.SetInOrder(gbi, x => x.Name);
    }
  }
}
