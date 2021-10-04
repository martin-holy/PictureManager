using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.DataAdapters;
using SimpleDB;
using System.Collections.Generic;

namespace PictureManager.Domain.Models {
  public sealed class FavoriteFolders : BaseCatTreeViewCategory, ITable {
    public DataAdapter DataAdapter { get; }
    public List<IRecord> All { get; } = new List<IRecord>();

    public FavoriteFolders(Core core) : base(Category.FavoriteFolders) {
      DataAdapter = new FavoriteFoldersDataAdapter(core, this);
      Title = "Favorites";
      IconName = IconName.FolderStar;
      CanRenameItems = true;
      CanDeleteItems = true;
      CanMoveItem = true;
    }

    public void ItemCreate(Folder folder) {
      var ff = new FavoriteFolder(DataAdapter.GetNextId()) {
        Title = folder.Title,
        Folder = folder,
        Parent = this
      };

      var idx = CatTreeViewUtils.SetItemInPlace(this, ff);
      var allIdx = Core.GetAllIndexBasedOnTreeOrder(All, this, idx);

      All.Insert(allIdx, ff);
      DataAdapter.IsModified = true;
    }

    public override void ItemDelete(ICatTreeViewItem item) {
      if (item is not FavoriteFolder folder) return;

      // remove from the Tree
      item.Parent.Items.Remove(item);

      // remove from DB
      All.Remove(folder);

      folder.Folder = null;
      DataAdapter.IsModified = true;
    }
  }
}
