using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories;

public sealed class FavoriteFoldersTreeCategory : TreeCategory<FavoriteFolderM> {
  public static RelayCommand<FolderM> AddToFavoritesCommand { get; set; }

  public FavoriteFoldersTreeCategory(FavoriteFoldersDA da) :
    base(Res.IconFolderStar, "Favorites", (int)Category.FavoriteFolders) {
    DataAdapter = da;
    CanMoveItem = true;
    AddToFavoritesCommand = new(da.ItemCreate, null, "Add to Favorites");
  }

  public override void OnItemSelected(object o) =>
    Core.FoldersM.TreeCategory.ScrollTo((o as FavoriteFolderM)?.Folder);
}