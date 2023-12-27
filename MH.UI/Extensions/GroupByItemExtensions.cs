using MH.UI.Controls;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Extensions;

public static class GroupByItemExtensions {
  public static GroupByItem<T> InGroup<T>(
    this IEnumerable<GroupByItem<T>> items, IListItem item, Func<T, object, bool> groupByFunc, bool isGroup = true) =>
    new(item, items, groupByFunc) { IsGroup = isGroup };

  public static IEnumerable<GroupByItem<TWhat>> GroupByParent<TWhat, TBy>(
    this IEnumerable<GroupByItem<TWhat>> items, Func<TWhat, object, bool> groupByFunc)
    where TBy : class, IListItem =>
    items
      .GroupBy(x => (x.Data as ITreeItem)?.Parent as TBy)
      .OrderBy(x => x.Key == null)
      .ThenBy(x => x.Key?.Name)
      .SelectMany(x => x.Key == null
        ? x.Select(y => y)
        : new[] { x.InGroup(x.Key, groupByFunc, false) });

  public static IEnumerable<GroupByItem<TWhat>> ToGroupByItems<TWhat, TBy>(
    this IEnumerable<TBy> items, Func<TWhat, object, bool> groupByFunc)
    where TWhat : class where TBy : IListItem =>
    items.Select(x => new GroupByItem<TWhat>(x, groupByFunc));
}