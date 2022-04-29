using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;

namespace MH.Utils {
  public static class Tree {
    public static T GetTopParent<T>(T item) where T : ITreeItem {
      var top = item;
      var parent = item?.Parent;

      while (parent is T t) {
        top = t;
        parent = parent.Parent;
      }

      return top;
    }

    public static void GetThisAndItemsRecursive<T>(object root, ref List<T> output) {
      output.Add((T)root);
      if (root is not ITreeItem treeItem) return;
      foreach (var item in treeItem.Items)
        GetThisAndItemsRecursive(item, ref output);
    }

    public static void GetThisAndParentRecursive<T>(T self, ref List<T> output) where T : ITreeItem {
      output.Add(self);
      var parent = self.Parent;
      while (parent is T t) {
        output.Add(t);
        parent = parent.Parent;
      }
    }

    public static string GetFullName<T>(T self, string separator, Func<T, string> nameSelector) where T : ITreeItem {
      var list = new List<T>();
      GetThisAndParentRecursive(self, ref list);
      list.Reverse();
      return string.Join(separator, list.Select(nameSelector));
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
  }
}
