using PictureManager.Domain.Models;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Category|GroupItems
  /// </summary>
  public class CategoryGroupsDataAdapter : DataAdapter {
    private readonly Core _core;
    private readonly CategoryGroupsM _model;

    public CategoryGroupsDataAdapter(Core core, CategoryGroupsM model) : base("CategoryGroups", core.Sdb) {
      _core = core;
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.All, ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      _model.All.Add(new(int.Parse(props[0]), props[1], (Category)int.Parse(props[2])) { Csv = props });
    }

    private static string ToCsv(CategoryGroupM categoryGroup) =>
      string.Join("|",
        categoryGroup.Id.ToString(),
        categoryGroup.Name,
        (int)categoryGroup.Category,
        string.Join(",", categoryGroup.Items.Select(x => ((IRecord)x).Id)));

    public override void LinkReferences() {
      foreach (var cg in _model.All) {
        if (string.IsNullOrEmpty(cg.Csv[3])) continue;

        var items = new List<int>();
        items.AddRange(cg.Csv[3].Split(',').Select(int.Parse));

        switch (cg.Category) {
          case Category.People:
            cg.Parent = _core.PeopleM;
            _core.PeopleM.Items.Add(cg);
            foreach (var item in items.Select(id => _core.PeopleM.AllDic[id])) {
              item.Parent = cg;
              cg.Items.Add(item);
            }

            break;

          case Category.Keywords:
            cg.Parent = _core.KeywordsM;
            _core.KeywordsM.Items.Add(cg);
            foreach (var item in items.Select(id => _core.KeywordsM.AllDic[id])) {
              item.Parent = cg;
              cg.Items.Add(item);
            }
              
            break;
        }

        cg.Items.CollectionChanged += _model.GroupItems_CollectionChanged;
      }
    }
  }
}
