using System.Collections.Specialized;
using System.Linq;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class FavoriteFoldersTreeVM : CatTreeViewCategoryBase {
    public FavoriteFoldersM Model { get; }

    public static RelayCommand<FolderTreeVM> AddToFavoritesCommand { get; } =
      new(item => App.Core.FavoriteFoldersM.ItemCreate(item.Model), item => item != null);

    public FavoriteFoldersTreeVM(FavoriteFoldersM model) : base(Category.FavoriteFolders, "Favorites") {
      Model = model;

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

    protected override void ModelItemRename(ICatTreeViewItem item, string name) =>
      Model.ItemRename((FavoriteFolderM)ToModel(item), name);

    protected override void ModelItemDelete(ICatTreeViewItem item) =>
      Model.ItemDelete((FavoriteFolderM)ToModel(item));

    protected override string ValidateNewItemName(ICatTreeViewItem root, string name) =>
      Model.ItemCanRename(name) ? null : $"{name} item already exists!";

    public override string GetTitle(object item) =>
      (item as FavoriteFolderTreeVM)?.Model.Title;

    private static object ToModel(object item) =>
      item switch {
        FavoriteFolderTreeVM x => x.Model,
        FavoriteFoldersTreeVM x => x.Model,
        _ => null
      };

    /*public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) =>
      Model.ItemMove(((FavoriteFolderTreeVM)item).Model,((FavoriteFolderTreeVM)dest).Model, aboveDest);*/
  }
}
