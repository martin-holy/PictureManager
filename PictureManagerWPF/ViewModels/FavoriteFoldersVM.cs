using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public static class FavoriteFoldersVM {
    public static RelayCommand<FolderM> AddToFavoritesCommand { get; } = new(
      item => App.Core.FavoriteFoldersM.ItemCreate(App.Core.FavoriteFoldersM, item));
  }
}
