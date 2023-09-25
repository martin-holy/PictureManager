using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories;

public sealed class FavoriteFoldersTreeCategory : TreeCategory<FavoriteFolderM> {
  public static RelayCommand<FolderM> AddToFavoritesCommand =>
    new(Core.Db.FavoriteFolders.ItemCreate);

  public FavoriteFoldersTreeCategory() : base(Res.IconFolderStar, "Favorites", (int)Category.FavoriteFolders) {
    DataAdapter = Core.Db.FavoriteFolders = new(this);
    CanMoveItem = true;
  }

  public override void OnItemSelected(object o) =>
    Core.FoldersM.TreeCategory.ScrollTo((o as FavoriteFolderM)?.Folder);
}