using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MH.Utils;

public static class Tree {
  public static bool IsFullyExpanded(this ITreeItem self) =>
    self.IsExpanded && (self.Parent == null || IsFullyExpanded(self.Parent));

  public static void ExpandTo(this ITreeItem self) {
    var items = self.GetThisAndParents().ToList();

    // don't expand this if Items are empty or it's just placeholder
    if (self.Items.Count == 0 || self.Items[0]?.Parent == null)
      items.Remove(self);

    items.Reverse();

    foreach (var item in items)
      item.IsExpanded = true;
  }

  public static T FindItem<T>(IEnumerable<T> items, Func<T, bool> equals) where T : class, ITreeItem {
    foreach (var item in items) {
      if (equals(item))
        return item;

      var res = FindItem(item.Items.OfType<T>(), equals);
      if (res != null) return res;
    }

    return default;
  }

  public static List<T> GetBranch<T>(this T item) where T : class, ITreeItem {
    var items = new List<T>();

    while (item != null) {
      items.Add(item);
      item = item.Parent as T;
    }

    items.Reverse();

    return items;
  }

  public static int GetIndex(this ITreeItem item, ITreeItem parent) {
    int index = 0;
    bool found = false;
    GetIndex(item, parent, ref index, ref found);
    return found ? index : -1;
  }

  // TODO do not count hidden items
  public static void GetIndex(ITreeItem item, ITreeItem parent, ref int index, ref bool found) {
    if (ReferenceEquals(item, parent)) {
      found = true;
      return;
    }
      
    if (parent.Items == null) return;

    foreach (var pItem in parent.Items) {
      index++;
      if (ReferenceEquals(item, pItem)) {
        found = true;
        break;
      }
      if (!pItem.IsExpanded) continue;
      GetIndex(item, pItem, ref index, ref found);
      if (found) break;
    }
  }

  public static int GetLevel(this ITreeItem item) {
    var level = 0;
    var parent = item.Parent;

    while (parent != null) {
      level++;
      parent = parent.Parent;
    }

    return level;
  }

  public static T GetParentOf<T>(ITreeItem item) where T : ITreeItem {
    var i = item;
    while (i != null) {
      if (i is T t) return t;
      i = i.Parent;
    }
    return default;
  }

  public static ITreeItem GetRoot(ITreeItem item) {
    var i = item;
    while (i != null) {
      if (i.Parent == null) return i;
      i = i.Parent;
    }
    return default;
  }

  public static IEnumerable<T> Flatten<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> elementSelector) {
    var stack = new Stack<IEnumerator<T>>();
    var e = source.GetEnumerator();
    try {
      while (true) {
        while (e.MoveNext()) {
          var item = e.Current;
          if (item == null) continue;
          yield return item;
          var elements = elementSelector(item);
          if (elements == null) continue;
          stack.Push(e);
          e = elements.GetEnumerator();
        }

        if (stack.Count == 0) break;
        e.Dispose();
        e = stack.Pop();
      }
    }
    finally {
      e.Dispose();
      while (stack.Count != 0)
        stack.Pop().Dispose();
    }
  }

  public static IEnumerable<T> Flatten<T>(this IEnumerable<T> items) where T : ITreeItem =>
    items.Flatten(x => x.Items.Cast<T>());

  public static IEnumerable<T> Flatten<T>(this T item) where T : ITreeItem =>
    new[] { item }.Concat(item.Items.Cast<T>().Flatten());

  public static IEnumerable<T> GetThisAndParents<T>(this T item) where T : class, ITreeItem {
    while (item != null) {
      yield return item;
      item = item.Parent as T;
    }
  }

  public static string GetFullName<T>(this T self, string separator, Func<T, string> nameSelector) where T : class, ITreeItem {
    var list = self.GetThisAndParents().ToList();
    list.Reverse();
    return string.Join(separator, list.Select(nameSelector));
  }

  public static bool HasThisParent(this ITreeItem self, ITreeItem parent) {
    var p = self.Parent;
    while (p != null) {
      if (ReferenceEquals(p, parent))
        return true;
      p = p.Parent;
    }

    return false;
  }

  public static void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest) {
    var relative = item.GetType() == dest.GetType();
    var newParent = relative
      ? dest.Parent
      : dest;

    if (newParent == null) return;

    if (!item.Parent.Equals(newParent)) {
      item.Parent.Items.Remove(item);
      item.Parent = newParent;
    }

    if (relative)
      newParent.Items.SetRelativeTo(item, dest, aboveDest);
    else
      SetInOrder(newParent.Items, item, x => x.Name);
  }

  public static void SetExpanded<T>(this ITreeItem self, bool value) where T : ITreeItem {
    if (self.IsExpanded != value)
      self.IsExpanded = value;
    if (self.Items == null) return;
    foreach (var item in self.Items.OfType<T>())
      item.SetExpanded<T>(value);
  }

  public static int SetInOrder<T>(ObservableCollection<T> collection, T item, Func<T, string> keySelector) {
    int newIdx;
    var strB = keySelector(item);
    var itemIsGroup = item is ITreeGroup;

    for (newIdx = 0; newIdx < collection.Count; newIdx++) {
      var compareItem = collection[newIdx];
      var compareItemIsGroup = compareItem is ITreeGroup;

      if (itemIsGroup && !compareItemIsGroup)
        break;

      if (!itemIsGroup && compareItemIsGroup)
        continue;

      var strA = keySelector(compareItem);
      var cRes = string.Compare(strA, strB, StringComparison.CurrentCultureIgnoreCase);
      if (collection[newIdx].Equals(item) || cRes < 0) continue;

      break;
    }

    var oldIdx = collection.IndexOf(item);
    if (oldIdx < 0)
      collection.Insert(newIdx, item);
    else if (oldIdx != newIdx) {
      if (newIdx > oldIdx) newIdx--;
      collection.Move(oldIdx, newIdx);
    }

    return newIdx;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="root"></param>
  /// <param name="path">full or partial path with no separator on the end</param>
  /// <param name="separator"></param>
  /// <param name="comparison"></param>
  /// <returns></returns>
  public static ITreeItem GetByPath(ITreeItem root, string path, char separator, StringComparison comparison = StringComparison.CurrentCultureIgnoreCase) {
    if (string.IsNullOrEmpty(path)) return null;

    var rootFullPath = GetFullName(root, separator.ToString(), x => x.Name);
    if (rootFullPath.Equals(path, comparison)) return root;

    var parts = (path.StartsWith(rootFullPath, comparison)
        ? path[(rootFullPath.Length + 1)..]
        : path)
      .Split(separator);

    foreach (var part in parts) {
      var item = root.Items.SingleOrDefault(x => x.Name.Equals(part, comparison));
      if (item == null) return null;
      root = item;
    }

    return root;
  }

  public static T FindChild<T>(IEnumerable<ITreeItem> items, Func<T, bool> equals) {
    foreach (var item in items) {
      if (equals((T)item))
        return (T)item;

      var res = FindChild(item.Items, equals);
      if (res != null) return res;
    }

    return default;
  }

  public static IEnumerable<TItem> AsTree<TItem, TGroup, TSort>(this IEnumerable<TItem> items, Func<TGroup, TSort> orderBy)
    where TItem : class, ITreeItem where TGroup : class, ITreeItem {
    var dic = items.ToDictionary(x => (TGroup)x.Data, x => x);

    foreach (var item in dic.OrderBy(x => orderBy(x.Key))) {
      if (item.Key.Parent is not TGroup parent) {
        yield return item.Value;
        continue;
      }

      item.Value.Parent = dic[parent];
      item.Value.Parent.Items.Add(item.Value);
    }
  }

  public static T GetBranchEndOfType<T>(this T item) where T : class, ITreeItem {
    while (item.Items.OfType<T>().Any())
      item = item.Items.OfType<T>().First();

    return item;
  }

  public static T GetNextBranchEndOfType<T>(this T current) where T : class, ITreeItem {
    if (current == null) return default;

    if (current.Items.OfType<T>().Any())
      return current.GetBranchEndOfType();

    var parent = current.Parent;
    while (parent != null) {
      int index = parent.Items.IndexOf(current);
      if (parent.Items.Skip(index + 1).OfType<T>().FirstOrDefault()?.GetBranchEndOfType() is { } next)
        return next;

      current = parent as T;
      parent = current?.Parent;
    }

    return default;
  }

  public static List<T> Toggle<T>(this List<T> list, T item) where T : class, ITreeItem {
    list ??= new();

    if (list.SelectMany(x => x.GetThisAndParents()).Any(x => ReferenceEquals(x, item))) {
      list.Remove(item);
      return list.Count == 0 ? null : list;
    }

    // remove possible redundant items
    // example: if new item is "Weather/Sunny" item "Weather" is redundant
    foreach (var newItem in item.GetThisAndParents())
      list.Remove(newItem);

    list.Add(item);

    return list;
  }

  public static IEnumerable<string> ToStrings<T>(this IEnumerable<T> items, Func<T, string> nameSelector)
    where T : class, ITreeItem =>
    items
      .EmptyIfNull()
      .SelectMany(x => x.GetThisAndParents())
      .Distinct()
      .OrderBy(x => x.GetFullName(".", nameSelector))
      .Select(nameSelector);
}