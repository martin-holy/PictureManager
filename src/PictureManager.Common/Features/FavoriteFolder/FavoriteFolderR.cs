using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Folder;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Features.FavoriteFolder;

/// <summary>
/// DB fields: ID|Folder|Title
/// </summary>
public class FavoriteFolderR : TreeDataAdapter<FavoriteFolderM> {
  private readonly CoreR _coreR;

  public FavoriteFolderTreeCategory Tree { get; }

  public FavoriteFolderR(CoreR coreR) : base(coreR, "FavoriteFolders", 3) {
    _coreR = coreR;
    IsDriveRelated = true;
    Tree = new(this);
  }

  public override Dictionary<string, IEnumerable<FavoriteFolderM>> GetAsDriveRelated() =>
    CoreR.GetAsDriveRelated(Tree.Items.Cast<FavoriteFolderM>(), x => x.Folder);

  public override FavoriteFolderM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[2], FolderR.Dummy);

  public override string ToCsv(FavoriteFolderM ff) =>
    string.Join("|",
      ff.GetHashCode().ToString(),
      ff.Folder.GetHashCode().ToString(),
      ff.Name);

  public override void LinkReferences() {
    Tree.Items.Clear();

    foreach (var (ff, csv) in AllCsv) {
      ff.Folder = _coreR.Folder.AllDict[int.Parse(csv[1])];
      ff.Parent = Tree;
      Tree.Items.Add(ff);
    }
  }

  public void ItemCreate(FolderM folder) =>
    TreeItemCreate(new(GetNextId(), folder.Name, folder) { Parent = Tree });

  public void ItemDeleteByFolder(FolderM folder) {
    if (All.SingleOrDefault(x => ReferenceEquals(x.Folder, folder)) is { } ff)
      ItemDelete(ff);
  }

  protected override void OnItemDeleted(object sender, FavoriteFolderM item) {
    item.Parent!.Items.Remove(item);
  }
}