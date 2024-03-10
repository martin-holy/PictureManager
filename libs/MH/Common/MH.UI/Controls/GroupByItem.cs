using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Controls;

public class GroupByItem<T>(IListItem data, Func<T, object, bool> fit) : TreeItem(null, data) {
  public bool IsGroup { get; set; }

  public GroupByItem(IListItem data, IEnumerable<ITreeItem> items, Func<T, object, bool> fit) : this(data, fit) {
    AddItems(items);
  }

  public bool Fit(T item) =>
    fit(item, Data);

  // TODO remove items as well
  public void Update(GroupByItem<T>[] items) {
    var newItems = Tree.FindItem(items, x => ReferenceEquals(x.Data, Data))?.Items.ToArray();
    if (newItems == null) return;
    var itemItems = Items.Cast<GroupByItem<T>>().ToArray();

    foreach (var itemItem in itemItems)
      itemItem.Update(items);

    foreach (var newItem in newItems) {
      if (itemItems.Any(x => ReferenceEquals(x.Data, newItem.Data)))
        continue;

      Items.SetInOrder(newItem, x => x.Data is IListItem d ? d.Name : string.Empty);
    }
  }
}

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
      .SelectMany(x =>
        x.Key == null
          ? x.Select(y => y)
          : new[] { x.InGroup(x.Key, groupByFunc, false) });

  public static IEnumerable<GroupByItem<TWhat>> ToGroupByItems<TWhat, TBy>(
    this IEnumerable<TBy> items, Func<TWhat, object, bool> groupByFunc)
    where TWhat : class where TBy : IListItem =>
    items.Select(x => new GroupByItem<TWhat>(x, groupByFunc));
}