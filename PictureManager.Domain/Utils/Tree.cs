using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Interfaces;

namespace PictureManager.Domain.Utils {
  public static class Tree {
    public delegate void OnItemsChanged(ObservableCollection<object> src, ObservableCollection<object> dest, ITreeBranch parent, OnItemsChanged onItemsChanged);

    public static void GetThisAndItemsRecursive<T>(object root, ref List<T> output) {
      output.Add((T)root);
      if (root is not ITreeBranch branch) return;
      foreach (var item in branch.Items)
        GetThisAndItemsRecursive(item, ref output);
    }

    public static void GetThisAndParentRecursive<T>(object self, ref List<T> output) {
      output.Add((T)self);
      var parent = (self as ITreeLeaf)?.Parent;
      while (parent is T t) {
        output.Add(t);
        parent = (parent as ITreeLeaf)?.Parent;
      }
    }

    public static string GetFullName<T>(T self, string separator, Func<T, string> nameSelector) {
      var list = new List<T>();
      GetThisAndParentRecursive(self, ref list);
      list.Reverse();
      return string.Join(separator, list.Select(nameSelector));
    }

    public static void ItemMove<T>(T item, object dest, bool aboveDest, Func<object, string> keySelector) where T : ITreeLeaf {
      if ((dest is T and ITreeLeaf { Parent: { } } x ? x.Parent : dest) is not ITreeBranch destParent) return;
      
      if (!item.Parent.Equals(destParent)) {
        (item.Parent as ITreeBranch)?.Items.Remove(item);
        item.Parent = destParent;
      }

      if (dest is T)
        destParent.Items.SetRelativeTo(item, dest, aboveDest);
      else
        destParent.Items.SetInOrder(item, keySelector);
    }

    public static void SyncCollection<TSrc, TDest>(ObservableCollection<object> src, ObservableCollection<object> dest,
      ITreeBranch parent, Func<TSrc, TDest, bool> itemsEquals, Func<TSrc, TDest> getDestItem) where TDest : class, ITreeLeaf {
      // Remove
      foreach (var o in dest.OfType<TDest>().Where(d => !src.OfType<TSrc>().Any(s => itemsEquals(s, d))).ToArray()) {
        dest.Remove(o);
        o.Parent = null;
      }

      // Insert or Move
      for (var i = 0; i < src.Count; i++) {
        if (src[i] is not TSrc srcItem) continue;
        if (i < dest.Count && itemsEquals(srcItem, (TDest)dest[i])) continue;
        var destItem = getDestItem(srcItem);
        var oldIdx = dest.IndexOf(destItem);
        if (oldIdx < 0) {
          dest.Insert(i, destItem);
          destItem.Parent = parent;
        }
        else
          dest.Move(oldIdx, i);
      }
    }

    public static TDest GetDestItem<TSrc, TDest>(TSrc src, int srcIdx, Dictionary<int, TDest> destSrc, Func<TDest> createNew, OnItemsChanged onItemsChanged) {
      if (destSrc.TryGetValue(srcIdx, out var dest)) return dest;

      dest = createNew();
      destSrc.Add(srcIdx, dest);

      if (onItemsChanged == null || src is not ITreeBranch s || dest is not ITreeBranch d) return dest;

      s.Items.CollectionChanged += (_, _) => onItemsChanged(s.Items, d.Items, d, onItemsChanged);
      onItemsChanged(s.Items, d.Items, d, onItemsChanged);

      return dest;
    }

    // TODO temporary until I change ICatTreeViewItem to implement ITreeBranch
    #region Temporary
    public delegate void OnItemsChangedCat(ObservableCollection<object> src, ObservableCollection<ICatTreeViewItem> dest, ICatTreeViewItem parent, OnItemsChangedCat onItemsChanged);

    public static void SyncCollection<TSrc, TDest>(ObservableCollection<object> src, ObservableCollection<ICatTreeViewItem> dest,
      ICatTreeViewItem parent, Func<TSrc, TDest, bool> itemsEquals, Func<TSrc, TDest> getDestItem) where TDest : class, ICatTreeViewItem {
      // Remove
      foreach (var o in dest.OfType<TDest>().Where(d => !src.OfType<TSrc>().Any(s => itemsEquals(s, d))).ToArray()) {
        dest.Remove(o);
        o.Parent = null;
      }

      // Insert or Move
      for (var i = 0; i < src.Count; i++) {
        if (src[i] is not TSrc srcItem) continue;
        if (i < dest.Count && itemsEquals(srcItem, (TDest)dest[i])) continue;
        var destItem = getDestItem(srcItem);
        var oldIdx = dest.IndexOf(destItem);
        if (oldIdx < 0) {
          dest.Insert(i, destItem);
          destItem.Parent = parent;
        }
        else
          dest.Move(oldIdx, i);
      }
    }

    public static TDest GetDestItemCat<TSrc, TDest>(TSrc src, int srcIdx, Dictionary<int, TDest> destSrc, Func<TDest> createNew, OnItemsChangedCat onItemsChanged) {
      if (destSrc.TryGetValue(srcIdx, out var dest)) return dest;

      dest = createNew();
      destSrc.Add(srcIdx, dest);

      if (onItemsChanged == null || src is not ITreeBranch s || dest is not ICatTreeViewItem d) return dest;

      s.Items.CollectionChanged += (_, _) => onItemsChanged(s.Items, d.Items, d, onItemsChanged);
      onItemsChanged(s.Items, d.Items, d, onItemsChanged);

      return dest;
    }
    #endregion
  }
}
