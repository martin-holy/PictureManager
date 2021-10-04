using PictureManager.Domain.Models;
using SimpleDB;
using System;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Category|GroupItems
  /// </summary>
  public class CategoryGroupsDataAdapter : DataAdapter {
    private readonly CategoryGroups _model;

    public CategoryGroupsDataAdapter(Core core, CategoryGroups model) : base(nameof(CategoryGroups), core.Sdb) {
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.All.Cast<CategoryGroup>(), ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      _model.All.Add(new CategoryGroup(int.Parse(props[0]), props[1], (Category)int.Parse(props[2])) { Csv = props });
    }

    public static string ToCsv(CategoryGroup categoryGroup) =>
      string.Join("|",
        categoryGroup.Id.ToString(),
        categoryGroup.Title,
        (int)categoryGroup.Category,
        string.Join(",", categoryGroup.Items.Cast<IRecord>().Select(x => x.Id)));
  }
}
