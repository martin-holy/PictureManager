using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;
using PictureManager.Domain.TreeCategories;
using System.Linq;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|Folder|Title
/// </summary>
public class FavoriteFoldersDataAdapter : TreeDataAdapter<FavoriteFolderM> {
  private readonly Db _db;

  public FavoriteFoldersTreeCategory Model { get; }

  public FavoriteFoldersDataAdapter(Db db) : base("FavoriteFolders", 3) {
    _db = db;
    Model = new(this);
  }

  public override void Save() =>
    SaveDriveRelated(Model.Items
      .Cast<FavoriteFolderM>()
      .GroupBy(x => Tree.GetParentOf<DriveM>(x.Folder))
      .ToDictionary(x => x.Key.Name, x => x.AsEnumerable()));

  public override FavoriteFolderM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[2]);

  public override string ToCsv(FavoriteFolderM ff) =>
    string.Join("|",
      ff.GetHashCode().ToString(),
      ff.Folder.GetHashCode().ToString(),
      ff.Name);

  public override void LinkReferences() {
    Model.Items.Clear();

    foreach (var (ff, csv) in AllCsv) {
      ff.Folder = _db.Folders.AllDict[int.Parse(csv[1])];
      ff.Parent = Model;
      Model.Items.Add(ff);
    }
  }

  public void ItemCreate(FolderM folder) =>
    TreeItemCreate(new(GetNextId(), folder.Name) {
      Parent = Model,
      Folder = folder
    });

  public void ItemDeleteByFolder(FolderM folder) {
    if (All.SingleOrDefault(x => x.Folder.Equals(folder)) is { } ff)
      ItemDelete(ff);
  }

  protected override void OnItemDeleted(FavoriteFolderM item) {
    item.Parent.Items.Remove(item);
    item.Folder = null;
  }
}