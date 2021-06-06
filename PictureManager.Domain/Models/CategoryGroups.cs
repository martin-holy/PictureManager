using System.Collections.Generic;
using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public class CategoryGroups : ITable {
    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new List<IRecord>();

    public void NewFromCsv(string csv) {
      // ID|Name|Category|GroupItems
      var props = csv.Split('|');
      if (props.Length != 4) return;
      var id = int.Parse(props[0]);
      All.Add(new CategoryGroup(id, props[1], (Category) int.Parse(props[2])) {Csv = props});
    }

    public void LinkReferences() {
      // ID|Name|Category|GroupItems
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
      Helper.IsModified = false;
    }

    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
    }

    public ICatTreeViewGroup GroupCreate(ICatTreeViewCategory cat, string name) {
      var group = new CategoryGroup(Helper.GetNextId(), name, cat.Category) {
        IconName = cat.CategoryGroupIconName,
        Parent = cat
      };

      var idx = CatTreeViewUtils.SetItemInPlace(cat, group);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(All, cat, idx);
      
      All.Insert(allIdx, group);
      SaveToFile();
      Core.Instance.Sdb.SaveIdSequences();

      return group;
    }

    public void GroupRename(ICatTreeViewGroup group, string name) {
      group.Title = name;

      var idx = CatTreeViewUtils.SetItemInPlace(group.Parent, group);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(All, group.Parent, idx);

      All.Move(group as CategoryGroup, allIdx);
      SaveToFile();
    }

    public void GroupDelete(CategoryGroup record) {
      All.Remove(record);
      SaveToFile();
    }

    public void GroupMove(CategoryGroup group, CategoryGroup dest, bool aboveDest) {
      All.Move(group, dest, aboveDest);
      SaveToFile();
    }
  }
}