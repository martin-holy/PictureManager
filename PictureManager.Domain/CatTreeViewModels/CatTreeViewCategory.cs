using System.Linq;

namespace PictureManager.Domain.CatTreeViewModels {
  public class CatTreeViewCategory : CatTreeViewItem, ICatTreeViewCategory {
    public Category Category { get; }
    public bool CanHaveGroups { get; set; }
    public bool CanHaveSubItems { get; set; }
    public bool CanCreateItems { get; set; }
    public bool CanRenameItems { get; set; }
    public bool CanDeleteItems { get; set; }
    public bool CanCopyItem { get; set; }
    public bool CanMoveItem { get; set; }
    public IconName CategoryGroupIconName { get; }

    public CatTreeViewCategory(Category category) {
      Category = category;
      CategoryGroupIconName = CatTreeViewUtils.GetCategoryGroupIconName(category);
    }

    public bool CanDrop(object src, ICatTreeViewItem dest) {
      return CatTreeViewUtils.CanDrop(src as ICatTreeViewItem, dest);
    }

    public void OnDrop(object src, ICatTreeViewItem dest, bool aboveDest, bool copy) {
      // groups
      if (src is ICatTreeViewGroup srcGroup && dest is ICatTreeViewGroup destGroup) {
        GroupMove(srcGroup, destGroup, aboveDest);
        return;
      }

      // items
      if (src is ICatTreeViewItem srcItem && dest is ICatTreeViewItem destItem)
        if (copy)
          ItemCopy(srcItem, destItem);
        else
          ItemMove(srcItem, destItem, aboveDest);
    }

    public bool CanCreateItem(ICatTreeViewItem item) {
      return CanCreateItems;
    }

    public bool CanRenameItem(ICatTreeViewItem item) {
      return CanRenameItems;
    }

    public bool CanDeleteItem(ICatTreeViewItem item) {
      return CanDeleteItems;
    }

    public bool CanSort(ICatTreeViewItem root) {
      return root.Items.Count > 0 && (CanCreateItems || CanRenameItems);
    }

    public string ValidateNewItemTitle(ICatTreeViewItem root, string name) {
      return root.Items.SingleOrDefault(x => !(x is ICatTreeViewGroup) && x.Title.Equals(name)) != null
        ? $"{name} item already exists!"
        : null;
    }

    public ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var item = new CatTreeViewItem {Title = name, Parent = root};
      CatTreeViewUtils.SetItemInPlace(root, item);

      return item;
    }

    public void ItemRename(ICatTreeViewItem item, string name) {
      item.Title = name;
      CatTreeViewUtils.SetItemInPlace(item.Parent, item);
    }

    public void ItemDelete(ICatTreeViewItem item) {
      item.Parent.Items.Remove(item);
    }

    public void ItemCopy(ICatTreeViewItem item, ICatTreeViewItem dest) {
      var copy = new CatTreeViewItem {Title = item.Title, Parent = dest};
      CatTreeViewUtils.SetItemInPlace(dest, copy);
    }

    public void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) {
      item.Parent.Items.Remove(item);

      if (dest is ICatTreeViewCategory || dest is ICatTreeViewGroup) {
        item.Parent = dest;
        CatTreeViewUtils.SetItemInPlace(dest, item);
      }
      else {
        item.Parent = dest.Parent;
        item.Parent.Items.Insert(item.Parent.Items.IndexOf(dest) + (aboveDest ? 0 : 1), item);
      }
    }

    public string ValidateNewGroupTitle(ICatTreeViewItem root, string name) {
      return root.Items.OfType<ICatTreeViewGroup>().SingleOrDefault(x => x.Title.Equals(name)) != null
        ? $"{name} group already exists!"
        : null;
    }

    public ICatTreeViewGroup GroupCreate(ICatTreeViewCategory cat, string name) {
      var group = new CatTreeViewGroup {Title = name, IconName = CategoryGroupIconName, Parent = cat};
      CatTreeViewUtils.SetItemInPlace(cat, group);

      return group;
    }

    public void GroupRename(ICatTreeViewGroup group, string name) {
      group.Title = name;
      CatTreeViewUtils.SetItemInPlace(group.Parent, group);
    }

    public void GroupDelete(ICatTreeViewGroup group) {
      // move Group items to the category
      foreach (var item in group.Items)
        Items.Add(item);

      group.Items.Clear();
      Items.Remove(group);
    }

    public void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) {
      group.Parent.Items.Move(group, dest, aboveDest);
    }
  }
}
