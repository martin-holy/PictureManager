using PictureManager.Domain.Models;
using SimpleDB;
using System;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|FolderId|Title
  /// </summary>
  public class FavoriteFoldersDataAdapter : DataAdapter {
    private readonly Core _core;
    private readonly FavoriteFolders _model;

    public FavoriteFoldersDataAdapter(Core core, FavoriteFolders model) : base(nameof(FavoriteFolders), core.Sdb) {
      _core = core;
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.All.Cast<FavoriteFolder>(), ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 3) throw new ArgumentException("Incorrect number of values.", csv);
      _model.All.Add(new FavoriteFolder(int.Parse(props[0])) { Title = props[2], Csv = props });
    }

    public static string ToCsv(FavoriteFolder favoriteFolder) =>
      string.Join("|",
        favoriteFolder.Id.ToString(),
        favoriteFolder.Folder.Id.ToString(),
        favoriteFolder.Title);

    public override void LinkReferences() {
      foreach (var item in _model.All.Cast<FavoriteFolder>()) {
        item.Folder = _core.Folders.AllDic[int.Parse(item.Csv[1])];
        item.ToolTip = item.Folder.FullPath;
        item.Parent = _model;
        _model.Items.Add(item);

        // csv array is not needed any more
        item.Csv = null;
      }
    }
  }
}
