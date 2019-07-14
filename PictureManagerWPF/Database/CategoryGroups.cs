using System.Collections.Generic;

namespace PictureManager.Database {
  public class CategoryGroups : ITable {
    public TableHelper Helper { get; set; }
    public Dictionary<int, IRecord> Records { get; set; } = new Dictionary<int, IRecord>();

    public void NewFromCsv(string csv) {
      // ID|Name|Category|GroupItems
      var props = csv.Split('|');
      if (props.Length != 4) return;
      var id = int.Parse(props[0]);
      Records.Add(id, new CategoryGroup(id, props[1], (Category) int.Parse(props[2])) {Csv = props});
    }

    public void LinkReferences(SimpleDB sdb) {
      // ID|Name|Category|GroupItems
      foreach (var item in Records) {
        var group = (CategoryGroup) item.Value;

        // reference to group items
        if (group.Csv[3] != string.Empty) {
          switch (group.Category) {
            case Category.People: {
              foreach (var itemId in group.Csv[3].Split(',')) {
                var p = (Person) sdb.Table<People>().Table.Records[int.Parse(itemId)];
                p.Parent = group;
                group.Items.Add(p);
              }

              break;
            }
            case Category.Keywords: {
              foreach (var itemId in group.Csv[3].Split(',')) {
                var k = (Keyword) sdb.Table<Keywords>().Table.Records[int.Parse(itemId)];
                k.Parent = group;
                group.Items.Add(k);
              }

              break;
            }
          }
        }

        // csv array is not needed any more
        group.Csv = null;
      }
    }
  }
}