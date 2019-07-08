using System.Collections.Generic;

namespace PictureManager.Database {
  public class CategoryGroups : ITable {
    public Dictionary<int, IRecord> Records { get; set; } = new Dictionary<int, IRecord>();

    public void NewFromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 4) return;
      var id = int.Parse(props[0]);
      Records.Add(id, new CategoryGroup(id, props[1], (Category) int.Parse(props[2])) {Csv = props});
    }

    public void LinkReferences(SimpleDB sdb) {
      foreach (var item in Records) {
        var group = (CategoryGroup) item.Value;

        // reference to group items
        if (group.Csv[3] != string.Empty)
          switch (group.Category) {
            case Category.People: {
              foreach (var itemId in group.Csv[4].Split(','))
                group.Items.Add((Person) sdb.Table<People>().Table.Records[int.Parse(itemId)]);
              break;
            }
            case Category.Keywords: {
              foreach (var itemId in group.Csv[4].Split(','))
                group.Items.Add((Keyword) sdb.Table<Keywords>().Table.Records[int.Parse(itemId)]);
              break;
            }
            /*case Category.SqlQueries: {
              foreach (var itemId in group.Csv[4].Split(','))
                group.Items.Add((SqlQuery) sdb.Table<SqlQueries>().Table.Records[int.Parse(itemId)]);
              break;
            }*/
          }

        // csv array is not needed any more
        group.Csv = null;
      }
    }
  }
}