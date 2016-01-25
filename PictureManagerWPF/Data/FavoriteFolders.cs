using System;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Properties;

namespace PictureManager.Data {
  public class FavoriteFolders : BaseItem {
    public ObservableCollection<FavoriteFolder> Items { get; set; }

    public FavoriteFolders() {
      Items = new ObservableCollection<FavoriteFolder>();
    }

    public void Load() {
      Items.Clear();
      foreach (string path in Settings.Default.FolderFavorites.OrderBy(x => x)) {
        int lio = path.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase);
        string label = path.Substring(lio + 1, path.Length - lio - 1);
        Items.Add(new FavoriteFolder {Title = label, FullPath = path, IconName = "appbar_folder"});
      }
    }
  }
}
