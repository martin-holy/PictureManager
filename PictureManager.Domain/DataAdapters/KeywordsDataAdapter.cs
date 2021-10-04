using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Parent
  /// </summary>
  public class KeywordsDataAdapter : DataAdapter {
    private readonly Keywords _model;

    public KeywordsDataAdapter(Core core, Keywords model) : base(nameof(Keywords), core.Sdb) {
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      _model.AllDic = new Dictionary<int, Keyword>();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.All.Cast<Keyword>(), ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 3) throw new ArgumentException("Incorrect number of values.", csv);
      var keyword = new Keyword(int.Parse(props[0]), props[1], null) { Csv = props };
      _model.All.Add(keyword);
      _model.AllDic.Add(keyword.Id, keyword);
    }

    public static string ToCsv(Keyword keyword) =>
      string.Join("|",
        keyword.Id.ToString(),
        keyword.Title,
        (keyword.Parent as Keyword)?.Id.ToString());

    public override void LinkReferences() {
      // MediaItems to the Keyword are added in LinkReferences on MediaItem

      // link hierarchical keywords
      foreach (var keyword in _model.All.Cast<Keyword>()) {
        // reference to parent and back reference to children
        if (!string.IsNullOrEmpty(keyword.Csv[2])) {
          keyword.Parent = _model.AllDic[int.Parse(keyword.Csv[2])];
          keyword.Parent.Items.Add(keyword);
        }

        // csv array is not needed any more
        keyword.Csv = null;
      }

      _model.Items.Clear();
      _model.LoadGroupsAndItems(_model.All);

      // group for keywords automatically added from MediaItems metadata
      _model.AutoAddedGroup = _model.Items.OfType<ICatTreeViewGroup>().SingleOrDefault(x =>
        x.Title.Equals("Auto Added")) ?? _model.GroupCreate(_model, "Auto Added");
    }
  }
}
