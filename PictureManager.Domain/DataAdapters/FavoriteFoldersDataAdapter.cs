using System;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|FolderId|Title
  /// </summary>
  public class FavoriteFoldersDataAdapter : DataAdapter {
    private readonly Core _core;
    private readonly FavoriteFoldersM _model;

    public FavoriteFoldersDataAdapter(Core core, FavoriteFoldersM model) : base("FavoriteFolders", core.Sdb) {
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
      if (props.Length != 3) throw new ArgumentException("Incorrect number of values.", csv);
      _model.All.Add(new FavoriteFolderM(int.Parse(props[0])) { Title = props[2], Csv = props });
    }

    private static string ToCsv(FavoriteFolderM ff) =>
      string.Join("|",
        ff.Id.ToString(),
        ff.Folder.Id.ToString(),
        ff.Title);

    public override void LinkReferences() {
      foreach (var item in _model.All) {
        item.Folder = _core.FoldersM.AllDic[int.Parse(item.Csv[1])];

        // csv array is not needed any more
        item.Csv = null;
      }
    }
  }
}
