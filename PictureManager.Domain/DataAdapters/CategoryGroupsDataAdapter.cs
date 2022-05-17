using System;
using System.Collections.Generic;
using System.Linq;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Category|GroupItems
  /// </summary>
  public class CategoryGroupsDataAdapter : DataAdapter<CategoryGroupM> {
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
      var category = (Category)int.Parse(props[2]);
      var group = new CategoryGroupM(int.Parse(props[0]), props[1], category, Res.CategoryToIconName(category));
      _model.All.Add(group);
      AllCsv.Add(group, props);
    }

    private static string ToCsv(CategoryGroupM categoryGroup) =>
      string.Join("|",
        categoryGroup.Id.ToString(),
        categoryGroup.Name,
        (int)categoryGroup.Category,
        string.Join(",", categoryGroup.Items.Select(x => x switch {
          KeywordM k => k.Id.ToString(),
          PersonM p => p.Id.ToString(),
          _ => throw new ArgumentException("Unexpected item in an group", x.Name)
        })));

    public override void LinkReferences() {
      foreach (var (cg, csv) in AllCsv) {
        var items = new List<int>();
        if (!string.IsNullOrEmpty(csv[3]))
          items.AddRange(csv[3].Split(',').Select(int.Parse));

        switch (cg.Category) {
          case Category.People:
            cg.Parent = _peopleM;
            _peopleM.Items.Add(cg);
            foreach (var item in items.Select(id => _peopleM.DataAdapter.AllId[id])) {
              item.Parent = cg;
              cg.Items.Add(item);
            }

            break;

          case Category.Keywords:
            cg.Parent = _keywordsM;
            _keywordsM.Items.Add(cg);
            foreach (var item in items.Select(id => _keywordsM.DataAdapter.AllId[id])) {
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
