using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class FavoriteFoldersTreeVM : CatTreeViewCategoryBase {
    private readonly FavoriteFoldersM _model;
    private readonly Dictionary<int, FavoriteFolderTreeVM> _all = new();

    public RelayCommand<FolderTreeVM> AddToFavoritesCommand { get; }

    public FavoriteFoldersTreeVM(FavoriteFoldersM model) : base(Category.FavoriteFolders, "Favorites") {
      _model = model;
      CanMoveItem = true;

      AddToFavoritesCommand = new(
        item => _model.ItemCreate(_model, item.Model),
        item => item != null);

      _model.Items.CollectionChanged += ModelItems_CollectionChanged;
      _model.FavoriteFolderDeletedEventHandler += (_, e) =>
        _all.Remove(e.Data.Id);

      // load items
      ModelItems_CollectionChanged(_model.Items, null);
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<FavoriteFolderM, FavoriteFolderTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => MH.Utils.Tree.GetDestItem(model, model.Id, _all, () => new(model, parent), null));
    }

    protected override void ModelItemRename(ICatTreeViewItem item, string name) =>
      _model.ItemRename((FavoriteFolderM)ToModel(item), name);

    protected override void ModelItemDelete(ICatTreeViewItem item) =>
      _model.ItemDelete((FavoriteFolderM)ToModel(item));

    public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) =>
      _model.ItemMove((FavoriteFolderM)ToModel(item), (ITreeLeaf)ToModel(dest), aboveDest);

    protected override string ValidateNewItemName(ICatTreeViewItem root, string name) =>
      _model.ItemCanRename(name)
        ? null
        : $"{name} item already exists!";

    public override string GetTitle(object item) =>
      (item as FavoriteFolderTreeVM)?.Model.Title;

    private static object ToModel(object item) =>
      item switch {
        FavoriteFolderTreeVM x => x.Model,
        FavoriteFoldersTreeVM x => x._model,
        _ => null
      };
  }
}
