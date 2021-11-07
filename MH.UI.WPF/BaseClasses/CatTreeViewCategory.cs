using System;
using MH.UI.WPF.Interfaces;

namespace MH.UI.WPF.BaseClasses {
  public class CatTreeViewCategory : CatTreeViewItem, ICatTreeViewCategory {
    public virtual void ItemCreate(ICatTreeViewItem root) => throw new NotImplementedException();
    public virtual void ItemRename(ICatTreeViewItem item) => throw new NotImplementedException();
    public virtual void ItemDelete(ICatTreeViewItem item) => throw new NotImplementedException();
    public virtual void GroupCreate(ICatTreeViewItem root) => throw new NotImplementedException();
    public virtual void GroupRename(ICatTreeViewGroup group) => throw new NotImplementedException();
    public virtual void GroupDelete(ICatTreeViewGroup group) => throw new NotImplementedException();
  }
}
