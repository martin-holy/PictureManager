using System;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class FavoriteFoldersM : TreeCategoryBase {
    public DataAdapter DataAdapter { get; set; }
    public ObservableCollection<FavoriteFolderM> All { get; } = new();

    public FavoriteFoldersM() : base(Res.IconFolderStar, Category.FavoriteFolders, "Favorites") {
      CanMoveItem = true;
    }

    protected override void ModelItemRename(ITreeItem item, string name) {
      item.Name = name;
      item.Parent.Items.SetInOrder(item, x => x.Name);
      DataAdapter.IsModified = true;
    }

    protected override void ModelItemDelete(ITreeItem item) {
      var ff = (FavoriteFolderM)item;
      ff.Parent.Items.Remove(ff);
      ff.Folder = null;
      All.Remove(ff);
      DataAdapter.IsModified = true;
    }

    public override void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest);
      DataAdapter.IsModified = true;
    }

    protected override string ValidateNewItemName(ITreeItem root, string name) =>
      All.Any(x => x.Name.Equals(name, StringComparison.CurrentCulture))
        ? $"{name} item already exists!"
        : null;

    public void ItemCreate(ITreeItem root, FolderM folder) {
      var ff = new FavoriteFolderM(DataAdapter.GetNextId(), folder.Name) {
        Parent = root,
        Folder = folder
      };

      Items.SetInOrder(ff, x => x.Name);
      All.Add(ff);
    }

    public void ItemDelete(FolderM folder) {
      if (All.SingleOrDefault(x => x.Folder.Equals(folder)) is { } ff)
        ModelItemDelete(ff);
    }
  }
}
