using System.Collections.Generic;
using System.Windows;

namespace PictureManager.Database {
  public class CategoryGroups : ITable {
    public AppCore ACore => (AppCore) Application.Current.Properties[nameof(AppProperty.AppCore)];
    public TableHelper Helper { get; set; }
    public List<CategoryGroup> All { get; } = new List<CategoryGroup>();

    public void NewFromCsv(string csv) {
      // ID|Name|Category|GroupItems
      var props = csv.Split('|');
      if (props.Length != 4) return;
      var id = int.Parse(props[0]);
      AddRecord(new CategoryGroup(id, props[1], (Category) int.Parse(props[2])) {Csv = props});
    }

    public void LinkReferences() {
      // ID|Name|Category|GroupItems
      foreach (var group in All) {
        // reference to group items
        switch (group.Category) {
          case Category.People: {
            group.Parent = ACore.People;
            if (group.Csv[3].Equals(string.Empty)) continue;

            foreach (var itemId in group.Csv[3].Split(',')) {
              var p = ACore.People.AllDic[int.Parse(itemId)];
              p.Parent = group;
              group.Items.Add(p);
            }

            break;
          }
          case Category.Keywords: {
            group.Parent = ACore.Keywords;
            if (group.Csv[3].Equals(string.Empty)) continue;

            foreach (var itemId in group.Csv[3].Split(',')) {
              var k = ACore.Keywords.AllDic[int.Parse(itemId)];
              k.Parent = group;
              group.Items.Add(k);
            }

            break;
          }
        }

        // csv array is not needed any more
        group.Csv = null;
      }
    }

    public void SaveToFile() {
      Helper.SaveToFile(All);
    }

    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
    }

    public void AddRecord(CategoryGroup record) {
      All.Add(record);
    }

    public void DeleteRecord(CategoryGroup record) {
      All.Remove(record);
      Helper.IsModifed = true;
    }
  }
}