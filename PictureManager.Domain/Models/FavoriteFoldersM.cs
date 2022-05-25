using System;
using System.Linq;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using MH.Utils.Interfaces;
using PictureManager.Domain.BaseClasses;
using PictureManager.Domain.DataAdapters;

namespace PictureManager.Domain.Models {
  public sealed class FavoriteFoldersM : TreeCategoryBase {
    public FavoriteFoldersDataAdapter DataAdapter { get; set; }

    public RelayCommand<FolderM> AddToFavoritesCommand { get; }

    public FavoriteFoldersM() : base(Res.IconFolderStar, Category.FavoriteFolders, "Favorites") {
      CanMoveItem = true;
      AddToFavoritesCommand = new(item => ItemCreate(this, item));
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
      DataAdapter.All.Remove(ff.Id);
      DataAdapter.IsModified = true;
    }

    public override void ItemMove(ITreeItem item, ITreeItem dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest);
      DataAdapter.IsModified = true;
    }

    protected override string ValidateNewItemName(ITreeItem root, string name) =>
      DataAdapter.All.Values.Any(x => x.Name.Equals(name, StringComparison.CurrentCulture))
        ? $"{name} item already exists!"
        : null;

    public void ItemCreate(ITreeItem root, FolderM folder) {
      var ff = new FavoriteFolderM(DataAdapter.GetNextId(), folder.Name) {
        Parent = root,
        Folder = folder
      };

      Items.SetInOrder(ff, x => x.Name);
      DataAdapter.All.Add(ff.Id, ff);
    }

    public void ItemDelete(FolderM folder) {
      if (DataAdapter.All.Values.SingleOrDefault(x => x.Folder.Equals(folder)) is { } ff)
        ModelItemDelete(ff);
    }
  }
}
