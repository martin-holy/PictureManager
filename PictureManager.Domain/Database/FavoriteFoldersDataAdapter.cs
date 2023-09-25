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
  private readonly FavoriteFoldersTreeCategory _model;

  public FavoriteFoldersDataAdapter(FavoriteFoldersTreeCategory model) : base("FavoriteFolders", 3) {
    _model = model;
  }

  public override void Save() =>
    SaveDriveRelated(_model.Items
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
    _model.Items.Clear();

    foreach (var (ff, csv) in AllCsv) {
      ff.Folder = Core.Db.Folders.AllDict[int.Parse(csv[1])];
      ff.Parent = _model;
      _model.Items.Add(ff);
    }
  }

  public void ItemCreate(FolderM folder) =>
    TreeItemCreate(new(GetNextId(), folder.Name) {
      Parent = _model,
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