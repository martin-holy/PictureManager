using System;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Domain.DataAdapters;
using PictureManager.Domain.Extensions;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class FavoriteFoldersM {
    public DataAdapter DataAdapter { get; }
    public ObservableCollection<FavoriteFolderM> All { get; } = new();

    public FavoriteFoldersM(Core core) {
      DataAdapter = new FavoriteFoldersDataAdapter(core, this);
    }

    public void ItemCreate(FolderM folder) {
      var ff = new FavoriteFolderM(DataAdapter.GetNextId()) {
        Title = folder.Name,
        Folder = folder
      };

      All.SetInOrder(ff, (x) => x.Title);
      DataAdapter.IsModified = true;
    }

    public void ItemMove(FavoriteFolderM item, FavoriteFolderM dest, bool aboveDest) {
      All.Move(item, dest, aboveDest);
      DataAdapter.IsModified = true;
    }

    public bool ItemCanRename(string name) =>
      !All.Any(x => x.Title.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void ItemRename(FavoriteFolderM item, string name) {
      item.Title = name;
      All.SetInOrder(item, (x) => x.Title);
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(FavoriteFolderM item) {
      All.Remove(item);
      item.Folder = null;
      DataAdapter.IsModified = true;
    }
  }
}
