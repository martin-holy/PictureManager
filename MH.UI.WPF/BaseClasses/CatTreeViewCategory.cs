using System;
using MH.UI.WPF.EventsArgs;
using MH.UI.WPF.Interfaces;
using MH.Utils;

namespace MH.UI.WPF.BaseClasses {
  public class CatTreeViewCategory : CatTreeViewItem, ICatTreeViewCategory {
    public event EventHandler<CatTreeViewItemDoppedEventArgs> OnAfterDrop = delegate { };
    public bool CanCopyItem { get; set; }
    public bool CanMoveItem { get; set; }

    public virtual void ItemCreate(ICatTreeViewItem root) => throw new NotImplementedException();
    public virtual void ItemRename(ICatTreeViewItem item) => throw new NotImplementedException();
    public virtual void ItemDelete(ICatTreeViewItem item) => throw new NotImplementedException();
    public virtual void ItemCopy(ICatTreeViewItem item, ICatTreeViewItem dest) => throw new NotImplementedException();
    public virtual void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) => throw new NotImplementedException();

    public virtual void GroupCreate(ICatTreeViewItem root) => throw new NotImplementedException();
    public virtual void GroupRename(ICatTreeViewGroup group) => throw new NotImplementedException();
    public virtual void GroupDelete(ICatTreeViewGroup group) => throw new NotImplementedException();
    public virtual void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) => throw new NotImplementedException();

    public virtual bool CanDrop(object src, ICatTreeViewItem dest) => CanDrop(src as ICatTreeViewItem, dest);

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
          ItemMove(srcItem, dest, aboveDest);

      OnAfterDrop(this, new(src, dest, aboveDest, copy));
    }

    public static bool CanDrop(ICatTreeViewItem src, ICatTreeViewItem dest) {
      if (src == null || dest == null || Equals(src, dest) ||
          src.Parent.Equals(dest) || dest.Parent?.Equals(src) == true ||
          (src is ICatTreeViewGroup && dest is not ICatTreeViewGroup)) return false;

      // if src or dest categories are null or they are not equal
      if (Tree.GetTopParent(src) is not ICatTreeViewCategory srcCat ||
          Tree.GetTopParent(dest) is not ICatTreeViewCategory destCat ||
          !Equals(srcCat, destCat)) return false;

      return true;
    }
  }
}
