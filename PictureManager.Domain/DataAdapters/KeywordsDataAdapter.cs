using PictureManager.Domain.Models;
using SimpleDB;
using System;
using System.Linq;
using PictureManager.Domain.Interfaces;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Parent
  /// </summary>
  public class KeywordsDataAdapter : DataAdapter {
    private readonly Core _core;
    private readonly KeywordsM _model;

    public KeywordsDataAdapter(Core core, KeywordsM model) : base("Keywords", core.Sdb) {
      _core = core;
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      _model.AllDic = new();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.All, ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 3) throw new ArgumentException("Incorrect number of values.", csv);
      var keyword = new KeywordM(int.Parse(props[0]), props[1], null) { Csv = props };
      _model.All.Add(keyword);
      _model.AllDic.Add(keyword.Id, keyword);
    }

    private static string ToCsv(KeywordM keyword) =>
      string.Join("|",
        keyword.Id.ToString(),
        keyword.Name,
        (keyword.Parent as KeywordM)?.Id.ToString());

    public override void LinkReferences() {
      // link hierarchical keywords
      foreach (var keyword in _model.All) {
        // reference to parent and back reference to children
        if (!string.IsNullOrEmpty(keyword.Csv[2])) {
          keyword.Parent = _model.AllDic[int.Parse(keyword.Csv[2])];
          ((ITreeBranch)keyword.Parent).Items.Add(keyword);
        }

        // csv array is not needed any more
        keyword.Csv = null;
      }

      // add loose keywords
      foreach (var keywordM in _model.All.Where(x => x.Parent == null)) {
        keywordM.Parent = _model;
        _model.Items.Add(keywordM);
      }

      // group for keywords automatically added from MediaItems metadata
      var aag = _model.Items.OfType<CategoryGroupM>().SingleOrDefault(x => x.Name.Equals("Auto Added"));
      if (aag == null) {
        aag = _core.CategoryGroupsM.GroupCreate("Auto Added", Category.Keywords);
        _model.Items.Insert(0, aag);
      }
      _model.AutoAddedGroup = aag;
    }
  }
}
