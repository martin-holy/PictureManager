using System.Collections.Specialized;
using System.Linq;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class FavoriteFoldersTreeVM : BaseCatTreeViewCategory {
    public FavoriteFoldersM Model { get; }

    public FavoriteFoldersTreeVM(FavoriteFoldersM model) : base(Category.FavoriteFolders) {
      Model = model;
      Title = "Favorites";
      IconName = IconName.FolderStar;
      CanRenameItems = true;
      CanDeleteItems = true;
      CanMoveItem = true;

      Model.All.CollectionChanged += All_CollectionChanged;
      SyncItems();
    }

    private void All_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      SyncItems();
    }

    private void SyncItems() {
      foreach (var ffVM in Items.Cast<FavoriteFolderTreeVM>().Where(vm => !Model.All.Contains(vm.Model)).ToArray()) {
        Items.Remove(ffVM);
        ffVM.Model = null;
      }

      for (int i = 0; i < Model.All.Count; i++) {
        var ffM = Model.All[i];
        var ffVM = Items.Cast<FavoriteFolderTreeVM>().SingleOrDefault(vm => vm.Model == ffM);
        
        if (ffVM == null) {
          Items.Insert(i, new FavoriteFolderTreeVM(ffM, this));
          continue;
        }

        var ffVMIdx = Items.IndexOf(ffVM);
        if (ffVMIdx != i)
          Items.Move(ffVMIdx, i);
      }
    }

    public override string GetTitle(ICatTreeViewItem item) => ((FavoriteFolderTreeVM)item).Model.Title;

    public override void SetTitle(ICatTreeViewItem item, string title) => ((FavoriteFolderTreeVM)item).Model.Title = title;

    public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) =>
      Model.ItemMove(((FavoriteFolderTreeVM)item).Model,((FavoriteFolderTreeVM)dest).Model, aboveDest);

    public override void ItemRename(ICatTreeViewItem item, string name) =>
      Model.ItemRename(((FavoriteFolderTreeVM)item).Model, name);

    public override string ValidateNewItemTitle(ICatTreeViewItem root, string name) =>
      Model.ItemCanRename(name) ? null : $"{name} item already exists!";

    public override void ItemDelete(ICatTreeViewItem item) =>
      Model.ItemDelete(((FavoriteFolderTreeVM)item).Model);
  }
}
