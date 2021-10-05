using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.Extensions;
using SimpleDB;
using System.Collections.Generic;

namespace PictureManager.Domain.Models {
  public sealed class CategoryGroups : ITable {
    public DataAdapter DataAdapter { get; }
    public List<IRecord> All { get; } = new List<IRecord>();

    public CategoryGroups(Core core) {
      DataAdapter = new CategoryGroupsDataAdapter(core, this);
    }

    public ICatTreeViewGroup GroupCreate(ICatTreeViewCategory cat, string name) {
      var group = new CategoryGroup(DataAdapter.GetNextId(), name, cat.Category) {
        IconName = cat.CategoryGroupIconName,
        Parent = cat
      };

      var idx = CatTreeViewUtils.SetItemInPlace(cat, group);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(All, cat, idx);

      All.Insert(allIdx, group);
      DataAdapter.IsModified = true;

      return group;
    }

    public void GroupRename(ICatTreeViewGroup group, string name) {
      group.Title = name;

      var idx = CatTreeViewUtils.SetItemInPlace(group.Parent, group);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(All, group.Parent, idx);

      All.Move(group as CategoryGroup, allIdx);
      DataAdapter.IsModified = true;
    }

    public void GroupDelete(CategoryGroup record) {
      All.Remove(record);
      DataAdapter.IsModified = true;
    }

    public void GroupMove(CategoryGroup group, CategoryGroup dest, bool aboveDest) {
      All.Move(group, dest, aboveDest);
      DataAdapter.IsModified = true;
    }
  }
}