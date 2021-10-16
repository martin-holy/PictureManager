using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Extensions;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public class BaseCatTreeViewCategory : CatTreeViewCategory {
    public BaseCatTreeViewCategory(Category category) : base(category) { }

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) => throw new System.NotImplementedException();

    public override void ItemRename(ICatTreeViewItem item, string name) {
      if (CatTreeViewUtils.GetTopParent(item) is not ICatTreeViewCategory cat || cat is not ITable table) return;

      cat.SetTitle(item, name);

      var idx = CatTreeViewUtils.SetItemInPlace(item.Parent, item);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(table.All, item.Parent, idx);

      table.All.Move(item as IRecord, allIdx);
      table.DataAdapter.IsModified = true;
    }

    public override void ItemDelete(ICatTreeViewItem item) => throw new System.NotImplementedException();

    public override void ItemCopy(ICatTreeViewItem item, ICatTreeViewItem dest) => throw new System.NotImplementedException();

    public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) {
      if (CatTreeViewUtils.GetTopParent(item) is not ICatTreeViewCategory cat || cat is not ITable table) return;

      var saveGroups = dest is ICatTreeViewCategory || dest is ICatTreeViewGroup || !Equals(item.Parent, dest.Parent);

      base.ItemMove(item, dest, aboveDest);
      var idx = item.Parent.Items.IndexOf(item);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(table.All, item.Parent, idx);

      table.All.Move(item as IRecord, allIdx);
      table.DataAdapter.IsModified = true;

      if (saveGroups)
        Core.Instance.CategoryGroupsM.DataAdapter.IsModified = true;
    }
  }
}
