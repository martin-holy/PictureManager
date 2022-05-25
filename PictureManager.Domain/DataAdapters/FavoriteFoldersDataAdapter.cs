using System.Linq;
using MH.Utils;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Folder|Title
  /// </summary>
  public class FavoriteFoldersDataAdapter : DataAdapter<FavoriteFolderM> {
    private readonly FavoriteFoldersM _model;
    private readonly FoldersM _foldersM;

    public FavoriteFoldersDataAdapter(FavoriteFoldersM model, FoldersM folders) : base("FavoriteFolders", 3) {
      _model = model;
      _foldersM = folders;
    }

    public override void Save() =>
      SaveDriveRelated(_model.Items
        .Cast<FavoriteFolderM>()
        .GroupBy(x => Tree.GetTopParent(x.Folder))
        .ToDictionary(x => x.Key.Name, x => x.AsEnumerable()));

    public override FavoriteFolderM FromCsv(string[] csv) =>
      new(int.Parse(csv[0]), csv[2]);

    public override string ToCsv(FavoriteFolderM ff) =>
      string.Join("|",
        ff.Id.ToString(),
        ff.Folder.Id.ToString(),
        ff.Name);

    public override void LinkReferences() {
      _model.Items.Clear();

      foreach (var (ff, csv) in AllCsv) {
        ff.Folder = _foldersM.DataAdapter.All[int.Parse(csv[1])];
        ff.Parent = _model;
        _model.Items.Add(ff);
      }
    }
  }
}
