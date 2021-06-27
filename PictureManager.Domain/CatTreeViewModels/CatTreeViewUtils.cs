using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.CatTreeViewModels {
  public static class CatTreeViewUtils {
    public static EventHandler OnAfterItemRename;
    public static EventHandler OnAfterItemDelete;
    public static EventHandler OnAfterOnDrop;
    public static EventHandler OnAfterSort;

    public static IconName GetCategoryGroupIconName(Category category) {
      return category switch {
        Category.FavoriteFolders => IconName.FolderStar,
        Category.Folders => IconName.Folder,
        Category.Ratings => IconName.Star,
        Category.People => IconName.PeopleMultiple,
        Category.FolderKeywords => IconName.Folder,
        Category.Keywords => IconName.TagLabel,
        Category.Filters => IconName.Filter,
        Category.Viewers => IconName.Eye,
        Category.SqlQueries => IconName.DatabaseSql,
        Category.MediaItemClips => IconName.MovieClapper,
        _ => IconName.Bug
      };
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
      var names = new List<string> { item.Title };
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
          if (i is not ICatTreeViewGroup) break;
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
      // if src or dest categories are null or they are not equal
      if (GetTopParent(src) is not ICatTreeViewCategory srcCat ||
          GetTopParent(dest) is not ICatTreeViewCategory destCat ||
        !Equals(srcCat, destCat)) return false;
      // if src and dest are groups
      if (src is ICatTreeViewGroup && dest is ICatTreeViewGroup) return true;
      // if src is item and src parent is not dest
      if (!(src is ICatTreeViewGroup) && !Equals(src.Parent, dest)) return true;

      return false;
    }

    public static void Sort(ICatTreeViewItem root) {
      if (root == null) return;

      // sort groups
      var groups = root.Items.OfType<ICatTreeViewGroup>()
        .OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
      foreach (var group in groups)
        root.Items.Move(root.Items.IndexOf(group), groups.IndexOf(group));

      // sort items
      var items = root.Items.Where(x => x is not ICatTreeViewGroup)
        .OrderBy(x => x.Title, StringComparer.CurrentCultureIgnoreCase).ToList();
      foreach (var item in items)
        root.Items.Move(root.Items.IndexOf(item), items.IndexOf(item));

      OnAfterSort?.Invoke(root, EventArgs.Empty);
    }
  }
}
