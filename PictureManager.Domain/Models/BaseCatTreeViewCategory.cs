using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public class BaseCatTreeViewCategory : CatTreeViewCategory, ICatTreeViewCategory {
    public BaseCatTreeViewCategory(Category category) : base(category) { }

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) => throw new System.NotImplementedException();

    public override void ItemRename(ICatTreeViewItem item, string name) {
      if (CatTreeViewUtils.GetTopParent(item) is not ICatTreeViewCategory cat || cat is not ITable table) return;

      item.Title = name;

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
        Core.Instance.CategoryGroups.DataAdapter.IsModified = true;
    }

    public override ICatTreeViewGroup GroupCreate(ICatTreeViewCategory cat, string name) =>
      Core.Instance.CategoryGroups.GroupCreate(cat, name);

    public override void GroupRename(ICatTreeViewGroup group, string name) =>
      Core.Instance.CategoryGroups.GroupRename(group, name);

    public override void GroupDelete(ICatTreeViewGroup group) {
      base.GroupDelete(group);
      Core.Instance.CategoryGroups.GroupDelete(group as CategoryGroup);
    }

    public override void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) {
      base.GroupMove(group, dest, aboveDest);
      Core.Instance.CategoryGroups.GroupMove(group as CategoryGroup, dest as CategoryGroup, aboveDest);
    }

    public void LoadGroupsAndItems(List<IRecord> items) {
      var recGroup = new Dictionary<int, CategoryGroup>();

      foreach (var group in Core.Instance.CategoryGroups.All.Cast<CategoryGroup>().Where(x => x.Category == Category)) {
        group.IconName = CatTreeViewUtils.GetCategoryGroupIconName(Category);
        group.Parent = this;
        Items.Add(group);

        if (!string.IsNullOrEmpty(group.Csv[3]))
          foreach (var itemId in group.Csv[3].Split(','))
            recGroup.Add(int.Parse(itemId), group);

        // csv array is not needed any more
        group.Csv = null;
      }

      foreach (var item in items.Cast<ICatTreeViewItem>().Where(x => x.Parent == null)) {
        recGroup.TryGetValue(((IRecord)item).Id, out var group);
        if (group != null) {
          item.Parent = group;
          group.Items.Add(item);
        }
        else {
          item.Parent = this;
          Items.Add(item);
        }
      }
    }
  }
}
