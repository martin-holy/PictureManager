using PictureManager.Domain.Extensions;
using System;
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

    public virtual bool CanDrop(object src, ICatTreeViewItem dest) => CatTreeViewUtils.CanDrop(src as ICatTreeViewItem, dest);

    public virtual void OnDrop(object src, ICatTreeViewItem dest, bool aboveDest, bool copy) {
      // groups
      if (src is ICatTreeViewGroup srcGroup && dest is ICatTreeViewGroup destGroup) {
        GroupMove(srcGroup, destGroup, aboveDest);
        return;
      }

      // items
      if (src is ICatTreeViewItem srcItem && dest != null)
        if (copy)
          ItemCopy(srcItem, dest);
        else
          this.ItemMove(srcItem, dest, aboveDest);
    }

    public virtual bool CanCreateItem(ICatTreeViewItem item) => CanCreateItems;

    public virtual bool CanRenameItem(ICatTreeViewItem item) => CanRenameItems;

    public virtual bool CanDeleteItem(ICatTreeViewItem item) => CanDeleteItems;

    public virtual bool CanSort(ICatTreeViewItem root) => root.Items.Count > 0 && (CanCreateItems || CanRenameItems);

    public virtual string ValidateNewItemTitle(ICatTreeViewItem root, string name) =>
      root.Items.SingleOrDefault(x => !(x is ICatTreeViewGroup) && x.Title.Equals(name, StringComparison.Ordinal)) != null
        ? $"{name} item already exists!"
        : null;

    public virtual ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var item = new CatTreeViewItem { Title = name, Parent = root };
      CatTreeViewUtils.SetItemInPlace(root, item);

      return item;
    }

    public virtual void ItemRename(ICatTreeViewItem item, string name) {
      item.Title = name;
      CatTreeViewUtils.SetItemInPlace(item.Parent, item);
    }

    public virtual void ItemDelete(ICatTreeViewItem item) => item.Parent.Items.Remove(item);

    public virtual void ItemCopy(ICatTreeViewItem item, ICatTreeViewItem dest) {
      var copy = new CatTreeViewItem { Title = item.Title, Parent = dest };
      CatTreeViewUtils.SetItemInPlace(dest, copy);
    }

    public virtual void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) {
      item.Parent.Items.Remove(item);

      if (dest is ICatTreeViewCategory or ICatTreeViewGroup) {
        item.Parent = dest;
        CatTreeViewUtils.SetItemInPlace(dest, item);
      }
      else {
        item.Parent = dest.Parent;
        item.Parent.Items.Insert(item.Parent.Items.IndexOf(dest) + (aboveDest ? 0 : 1), item);
      }
    }

    public virtual string GetTitle(ICatTreeViewItem item) => item.Title;
    public virtual void SetTitle(ICatTreeViewItem item, string title) => item.Title = title;
    public virtual string GetGroupTitle(ICatTreeViewGroup item) => item.Title;
    public virtual void SetGroupTitle(ICatTreeViewGroup item, string title) => item.Title = title;

    public virtual string ValidateNewGroupTitle(ICatTreeViewItem root, string name) =>
      root.Items.OfType<ICatTreeViewGroup>().SingleOrDefault(x => x.Title.Equals(name, StringComparison.Ordinal)) != null
        ? $"{name} group already exists!"
        : null;

    public virtual void GroupCreate(ICatTreeViewCategory cat, string name) {
      var group = new CatTreeViewGroup { Title = name, IconName = CategoryGroupIconName, Parent = cat };
      CatTreeViewUtils.SetItemInPlace(cat, group);
    }

    public virtual void GroupRename(ICatTreeViewGroup group, string name) {
      group.Title = name;
      CatTreeViewUtils.SetItemInPlace(group.Parent, group);
    }

    public virtual void GroupDelete(ICatTreeViewGroup group) {
      // move Group items to the category
      foreach (var item in group.Items)
        Items.Add(item);

      group.Items.Clear();
      Items.Remove(group);
    }

    public virtual void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) =>
      group.Parent.Items.Move(group, dest, aboveDest);
  }
}
