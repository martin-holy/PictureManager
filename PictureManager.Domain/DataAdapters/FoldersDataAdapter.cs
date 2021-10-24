using PictureManager.Domain.Models;
using SimpleDB;
using System;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Parent|IsFolderKeyword
  /// </summary>
  public class FoldersDataAdapter : DataAdapter {
    private readonly FoldersM _model;

    public FoldersDataAdapter(Core core, FoldersM model) : base("Folders", core.Sdb) {
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
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      var folder = new FolderM(int.Parse(props[0]), props[1], null) { Csv = props, IsFolderKeyword = props[3] == "1" };
      _model.All.Add(folder);
      _model.AllDic.Add(folder.Id, folder);
    }

    private static string ToCsv(FolderM folder) =>
      string.Join("|",
        folder.Id.ToString(),
        folder.Name,
        (folder.Parent as FolderM)?.Id.ToString() ?? string.Empty,
        folder.IsFolderKeyword ? "1" : string.Empty);

    public override void LinkReferences() {
      foreach (var folder in _model.All.Cast<FolderM>()) {
        // reference to Parent and back reference from Parent to SubFolder
        folder.Parent = !string.IsNullOrEmpty(folder.Csv[2]) ? _model.AllDic[int.Parse(folder.Csv[2])] : _model;
        folder.Parent.Items.Add(folder);
        // csv array is not needed any more
        folder.Csv = null;
      }
    }
  }
}
