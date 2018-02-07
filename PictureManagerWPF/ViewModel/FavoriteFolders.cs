using System;
using System.Linq;
using PictureManager.Properties;

namespace PictureManager.ViewModel {
  public sealed class FavoriteFolders : BaseCategoryItem {

    public FavoriteFolders() : base(Categories.FavoriteFolders) {
      Title = "Favorites";
      IconName = "appbar_folder_star";
    }

    public void Load() {
      Items.Clear();
      foreach (var path in Settings.Default.FolderFavorites.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x)) {
        var p = path.EndsWith("\\") ? path.Substring(0, path.Length - 1) : path;
        var lio = p.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase);
        var label = p.Substring(lio + 1, p.Length - lio - 1);
        Items.Add(new FavoriteFolder {Title = label, FullPath = p});
      }
    }

    public void Remove(string path) {
      var lines = Settings.Default.FolderFavorites.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).ToList();
      lines.Remove(path);
      Settings.Default.FolderFavorites = string.Join(Environment.NewLine, lines);
      Settings.Default.Save();
    }

    public void Add(string path) {
      if (Settings.Default.FolderFavorites.IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0) return;
      if (!Settings.Default.FolderFavorites.EndsWith(Environment.NewLine)) {
        Settings.Default.FolderFavorites += Environment.NewLine;
      }
      Settings.Default.FolderFavorites += path;
      Settings.Default.Save();
    }
  }
}
