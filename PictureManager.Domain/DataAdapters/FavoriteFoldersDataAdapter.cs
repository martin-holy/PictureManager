using System;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Folder|Title
  /// </summary>
  public class FavoriteFoldersDataAdapter : DataAdapter<FavoriteFolderM> {
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
      var favoriteFolder = new FavoriteFolderM(int.Parse(props[0]), props[2]);
      _model.All.Add(favoriteFolder);
      AllCsv.Add(favoriteFolder, props);
    }

    private static string ToCsv(FavoriteFolderM ff) =>
      string.Join("|",
        ff.Id.ToString(),
        ff.Folder.Id.ToString(),
        ff.Name);

    public override void LinkReferences() {
      _model.Items.Clear();

      foreach (var (ff, csv) in AllCsv) {
        ff.Folder = _foldersM.DataAdapter.AllId[int.Parse(csv[1])];
        ff.Parent = _model;
        _model.Items.Add(ff);
      }
    }
  }
}
