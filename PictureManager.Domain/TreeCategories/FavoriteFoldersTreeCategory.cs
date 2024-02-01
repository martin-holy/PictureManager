using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories;

public sealed class FavoriteFoldersTreeCategory : TreeCategory<FavoriteFolderM> {
  public static RelayCommand<FolderM> AddToFavoritesCommand =>
    new(Core.Db.FavoriteFolders.ItemCreate, null, "Add to Favorites");

  public FavoriteFoldersTreeCategory(FavoriteFoldersDA da) :
    base(Res.IconFolderStar, "Favorites", (int)Category.FavoriteFolders) {
    DataAdapter = da;
    CanMoveItem = true;
  }

  public override void OnItemSelected(object o) =>
    Core.FoldersM.TreeCategory.ScrollTo((o as FavoriteFolderM)?.Folder);
}