using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;

namespace PictureManager.Common.TreeCategories;

public sealed class FavoriteFoldersTreeCategory : TreeCategory<FavoriteFolderM> {
  public static RelayCommand<FolderM> AddToFavoritesCommand { get; set; }

  public FavoriteFoldersTreeCategory(FavoriteFolderR r) :
    base(Res.IconFolderStar, "Favorites", (int)Category.FavoriteFolders) {
    DataAdapter = r;
    CanMoveItem = true;
    AddToFavoritesCommand = new(r.ItemCreate, null, "Add to Favorites");
  }

  public override void OnItemSelected(object o) =>
    Core.R.Folder.Tree.ScrollTo((o as FavoriteFolderM)?.Folder);
}