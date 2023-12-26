using MH.UI.Controls;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Extensions;

public static class GroupByItemExtensions {
  public static CollectionViewGroupByItem<T> InGroup<T>(
    this IEnumerable<CollectionViewGroupByItem<T>> items, IListItem item, Func<T, object, bool> groupByFunc) =>
    new(item, items, groupByFunc) { IsGroup = true };

  public static IEnumerable<CollectionViewGroupByItem<TWhat>> InGroups<TWhat, TBy>(
    this IEnumerable<CollectionViewGroupByItem<TWhat>> items, Func<TWhat, object, bool> groupByFunc)
    where TBy : class, IListItem {
    var groups = items.GroupBy(x => (x.Data as ITreeItem)?.Parent as TBy).ToArray();
    return groups
      .Where(x => x.Key != null)
      .OrderBy(x => x.Key.Name)
      .Select(x => {
        var group = new CollectionViewGroupByItem<TWhat>(x.Key, groupByFunc);
        group.Items.AddItems(x.Cast<ITreeItem>().ToArray(), item => item.Parent = group);

        return group;
      })
      .Concat(groups.Where(x => x.Key == null).SelectMany(x => x));
  }

  public static IEnumerable<CollectionViewGroupByItem<TWhat>> ToGroupByItems<TWhat, TBy>(
    this IEnumerable<TBy> items, Func<TWhat, object, bool> groupByFunc)
    where TWhat : class where TBy : IListItem =>
    items.Select(x => new CollectionViewGroupByItem<TWhat>(x, groupByFunc));
}