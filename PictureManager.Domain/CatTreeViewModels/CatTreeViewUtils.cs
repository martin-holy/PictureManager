using System;
using System.Collections.Generic;

namespace PictureManager.Domain.CatTreeViewModels {
  public static class CatTreeViewUtils {
    public static EventHandler OnAfterItemRename;
    public static EventHandler OnAfterItemDelete;
    public static EventHandler OnAfterOnDrop;

    public static IconName GetCategoryGroupIconName(Category category) {
      switch (category) {
        case Category.FavoriteFolders: return IconName.FolderStar;
        case Category.Folders: return IconName.Folder;
        case Category.Ratings: return IconName.Star;
        case Category.People: return IconName.PeopleMultiple;
        case Category.FolderKeywords: return IconName.Folder;
        case Category.Keywords: return IconName.TagLabel;
        case Category.Filters: return IconName.Filter;
        case Category.Viewers: return IconName.Eye;
        case Category.SqlQueries: return IconName.DatabaseSql;
        case Category.MediaItemClips: return IconName.MovieClapper;
        default: return IconName.Bug;
      }
    }

    public static ICatTreeViewItem GetTopParent(ICatTreeViewItem item) {
      if (item == null) return null;
      while (true) {
        if (item.Parent == null) return item;
        item = item.Parent;
      }
    }

    public static void GetThisAndItemsRecursive(ICatTreeViewItem self, ref List<ICatTreeViewItem> items) {
      items.Add(self);
      foreach (var item in self.Items)
        GetThisAndItemsRecursive(item, ref items);
    }

    public static void GetThisAndParentRecursive(ICatTreeViewItem self, ref List<ICatTreeViewItem> items) {
      items.Add(self);
      var parent = self.Parent;
      while (parent != null) {
        items.Add(parent);
        parent = parent.Parent;
      }
    }

    public static string GetFullPath(ICatTreeViewItem item, string separator) {
      if (item == null) return null;
      var parent = item.Parent;
      var names = new List<string> {item.Title};
      while (parent != null) {
        if (parent is ICatTreeViewCategory) break;
        names.Add(parent.Title);
        parent = parent.Parent;
      }
      names.Reverse();

      return string.Join(separator, names);
    }

    public static void ExpandAll(ICatTreeViewItem root) {
      if (root.Items.Count == 0) return;
      root.IsExpanded = true;
      foreach (var item in root.Items)
        ExpandAll(item);
    }

    public static void ExpandTo(ICatTreeViewItem item) {
      // expand item as well if it has any sub item and not just placeholder
      if (item.Items.Count > 0 && item.Items[0].Title != null)
        item.IsExpanded = true;
      var parent = item.Parent;
      while (parent != null) {
        parent.IsExpanded = true;
        parent = parent.Parent;
      }
    }

    public static int SetItemInPlace(ICatTreeViewItem root, ICatTreeViewItem item) {
      if (root == null || item == null) return -1;

      root.Items.Remove(item);

      var itemIsGroup = item is ICatTreeViewGroup;
      var idx = 0;
      foreach (var i in root.Items) {
        if (itemIsGroup) {
          if (!(i is ICatTreeViewGroup)) break;
        }
        else {
          if (i is ICatTreeViewGroup) {
            idx++;
            continue;
          }
        }
        
        if (string.Compare(item.Title, i.Title, StringComparison.CurrentCultureIgnoreCase) < 0) break;
        idx++;
      }

      root.Items.Insert(idx, item);

      return idx;
    }

    public static bool CanDrop(ICatTreeViewItem src, ICatTreeViewItem dest) {
      // if src or dest are null or they are equal
      if (src == null || dest == null || Equals(src, dest)) return false;
      
      var srcCat = GetTopParent(src) as ICatTreeViewCategory;
      var destCat = GetTopParent(dest) as ICatTreeViewCategory;

      // if src or dest categories are null or they are not equal
      if (srcCat == null || destCat == null || !Equals(srcCat, destCat)) return false;
      // if src and dest are groups
      if (src is ICatTreeViewGroup && dest is ICatTreeViewGroup) return true;
      // if src is item and src parent is not dest
      if (!(src is ICatTreeViewGroup) && !Equals(src.Parent, dest)) return true;

      return false;
    }
  }
}
