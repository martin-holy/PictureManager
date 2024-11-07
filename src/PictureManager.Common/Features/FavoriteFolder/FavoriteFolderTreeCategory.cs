using MH.UI.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Common.Features.Folder;

namespace PictureManager.Common.Features.FavoriteFolder;

public sealed class FavoriteFolderTreeCategory : TreeCategory<FavoriteFolderM> {
  public static RelayCommand<FolderM> AddToFavoritesCommand { get; set; } = null!;

  public FavoriteFolderTreeCategory(FavoriteFolderR r) :
    base(new(), Res.IconFolderStar, "Favorites", (int)Category.FavoriteFolders, r) {
    CanMoveItem = true;
    AddToFavoritesCommand = new(x => r.ItemCreate(x!), x => x != null, null, "Add to Favorites");
  }

  public override void OnItemSelected(object o) =>
    Core.R.Folder.Tree.ScrollTo((o as FavoriteFolderM)?.Folder);
}