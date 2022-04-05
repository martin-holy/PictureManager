using System;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class FavoriteFoldersM : ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    public DataAdapter DataAdapter { get; set; }
    public ObservableCollection<FavoriteFolderM> All { get; } = new();
    public event EventHandler<ObjectEventArgs<FavoriteFolderM>> FavoriteFolderDeletedEventHandler = delegate { };

    private static string GetItemName(object item) =>
      item is FavoriteFolderM x
        ? x.Title
        : string.Empty;

    public void ItemCreate(ITreeBranch root, FolderM folder) {
      var ff = new FavoriteFolderM(DataAdapter.GetNextId()) {
        Parent = root,
        Title = folder.Name,
        Folder = folder
      };

      Items.SetInOrder(ff, GetItemName);
      All.Add(ff);
    }

    public void ItemMove(FavoriteFolderM item, ITreeLeaf dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest, GetItemName);
      DataAdapter.IsModified = true;
    }

    public bool ItemCanRename(string name) =>
      !All.Any(x => x.Title.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void ItemRename(FavoriteFolderM item, string name) {
      item.Title = name;
      item.Parent.Items.SetInOrder(item, GetItemName);
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(FavoriteFolderM item) {
      item.Parent.Items.Remove(item);
      item.Folder = null;
      All.Remove(item);
      FavoriteFolderDeletedEventHandler(this, new(item));
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(FolderM folder) {
      if (All.SingleOrDefault(x => x.Folder.Equals(folder)) is { } ff)
        ItemDelete(ff);
    }
  }
}
