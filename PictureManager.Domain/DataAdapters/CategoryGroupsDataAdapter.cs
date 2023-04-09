using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Category|GroupItems
  /// </summary>
  public class CategoryGroupsDataAdapter : DataAdapter<CategoryGroupM> {
    private readonly CategoryGroupsM _model;
    private readonly KeywordsM _keywordsM;
    private readonly PeopleM _peopleM;

    public CategoryGroupsDataAdapter(CategoryGroupsM model, KeywordsM k, PeopleM p) : base("CategoryGroups", 4) {
      _model = model;
      _keywordsM = k;
      _peopleM = p;
    }

    public override void Save() =>
      SaveToFile(_keywordsM.Items
        .OfType<CategoryGroupM>()
        .Concat(_peopleM.Items
          .OfType<CategoryGroupM>()));

    public override CategoryGroupM FromCsv(string[] csv) {
      var category = (Category)int.Parse(csv[2]);
      return new(int.Parse(csv[0]), csv[1], category, Res.CategoryToIconName(category));
    }

    public override string ToCsv(CategoryGroupM categoryGroup) =>
      string.Join("|",
        categoryGroup.Id.ToString(),
        categoryGroup.Name,
        (int)categoryGroup.Category,
        string.Join(",", categoryGroup.Items
          .OfType<IRecord>()
          .Select(x => x.Id.ToString())));

    public override void LinkReferences() {
      _peopleM.Items.Clear();
      _keywordsM.Items.Clear();

      foreach (var (cg, csv) in AllCsv) {
        var items = new List<int>();
        if (!string.IsNullOrEmpty(csv[3]))
          items.AddRange(csv[3].Split(',').Select(int.Parse));

        switch (cg.Category) {
          case Category.People:
            cg.Parent = _peopleM;
            _peopleM.Items.Add(cg);
            foreach (var item in items.Select(id => _peopleM.DataAdapter.All[id])) {
              item.Parent = cg;
              cg.Items.Add(item);
            }

            break;

          case Category.Keywords:
            cg.Parent = _keywordsM;
            _keywordsM.Items.Add(cg);
            foreach (var item in items.Select(id => _keywordsM.DataAdapter.All[id])) {
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
