using System;
using System.Linq;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Parent
  /// </summary>
  public class KeywordsDataAdapter : DataAdapter<KeywordM> {
    private readonly KeywordsM _model;
    private readonly CategoryGroupsM _categoryGroupsM;

    public KeywordsDataAdapter(SimpleDB.SimpleDB db, KeywordsM model, CategoryGroupsM cg)
      : base("Keywords", db) {
      _model = model;
      _categoryGroupsM = cg;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() =>
      SaveToFile(_model.All, ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 3) throw new ArgumentException("Incorrect number of values.", csv);
      var keyword = new KeywordM(int.Parse(props[0]), props[1], null);
      _model.All.Add(keyword);
      AllCsv.Add(keyword, props);
      AllId.Add(keyword.Id, keyword);
    }

    private static string ToCsv(KeywordM keyword) =>
      string.Join("|",
        keyword.Id.ToString(),
        keyword.Name,
        (keyword.Parent as KeywordM)?.Id.ToString());

    public override void LinkReferences() {
      // link hierarchical keywords
      foreach (var (keyword, csv) in AllCsv) {
        // reference to parent and back reference to children
        if (!string.IsNullOrEmpty(csv[2])) {
          keyword.Parent = AllId[int.Parse(csv[2])];
          keyword.Parent.Items.Add(keyword);
        }
      }

      // add loose keywords
      foreach (var keywordM in _model.All.Where(x => x.Parent == null)) {
        keywordM.Parent = _model;
        _model.Items.Add(keywordM);
      }

      // group for keywords automatically added from MediaItems metadata
      _model.AutoAddedGroup = _model.Items
        .OfType<CategoryGroupM>()
        .SingleOrDefault(x => x.Name.Equals("Auto Added"))
        ?? _categoryGroupsM.GroupCreate("Auto Added", Category.Keywords);
    }
  }
}
