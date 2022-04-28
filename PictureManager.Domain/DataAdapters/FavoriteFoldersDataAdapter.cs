using System;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Folder|Title
  /// </summary>
  public class FavoriteFoldersDataAdapter : DataAdapter {
    private readonly FavoriteFoldersM _model;
    private readonly FoldersM _foldersM;

    public FavoriteFoldersDataAdapter(SimpleDB.SimpleDB db, FavoriteFoldersM model, FoldersM f)
      : base("FavoriteFolders", db) {
      _model = model;
      _foldersM = f;
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
      _model.All.Add(
        new(int.Parse(props[0]), props[2]) {
          Csv = props
        });
    }

    private static string ToCsv(FavoriteFolderM ff) =>
      string.Join("|",
        ff.Id.ToString(),
        ff.Folder.Id.ToString(),
        ff.Name);

    public override void LinkReferences() {
      _model.Items.Clear();

      foreach (var item in _model.All) {
        item.Folder = _foldersM.AllDic[int.Parse(item.Csv[1])];
        item.Parent = _model;
        _model.Items.Add(item);

        // csv array is not needed any more
        item.Csv = null;
      }
    }
  }
}
