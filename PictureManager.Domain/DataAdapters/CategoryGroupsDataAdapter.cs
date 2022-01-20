using System;
using System.Collections.Generic;
using System.Linq;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Category|GroupItems
  /// </summary>
  public class CategoryGroupsDataAdapter : DataAdapter {
    private readonly CategoryGroupsM _model;
    private readonly KeywordsM _keywordsM;
    private readonly PeopleM _peopleM;

    public CategoryGroupsDataAdapter(SimpleDB.SimpleDB db, CategoryGroupsM model, KeywordsM k, PeopleM p)
      : base("CategoryGroups", db) {
      _model = model;
      _keywordsM = k;
      _peopleM = p;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() =>
      SaveToFile(_model.All, ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      _model.All.Add(
        new(int.Parse(props[0]), props[1], (Category)int.Parse(props[2])) {
          Csv = props
        });
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
            cg.Parent = _peopleM;
            _peopleM.Items.Add(cg);
            foreach (var item in items.Select(id => _peopleM.AllDic[id])) {
              item.Parent = cg;
              cg.Items.Add(item);
            }

            break;

          case Category.Keywords:
            cg.Parent = _keywordsM;
            _keywordsM.Items.Add(cg);
            foreach (var item in items.Select(id => _keywordsM.AllDic[id])) {
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
