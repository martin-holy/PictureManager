using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using DU = PictureManager.Domain.Utils;

namespace PictureManager.ViewModels.Tree {
  public sealed class ViewersTreeVM : BaseCatTreeViewCategory {
    public ViewersBaseVM BaseVM { get; }
    public readonly Dictionary<int, ViewerTreeVM> All = new();

    public ViewersTreeVM(ViewersBaseVM baseVM) : base(Category.Viewers) {
      BaseVM = baseVM;

      Title = "Viewers";
      IconName = IconName.Eye;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;

      BaseVM.Model.Items.CollectionChanged += ModelItems_CollectionChanged;
      BaseVM.Model.ViewerDeletedEvent += (_, e) => All.Remove(e.Viewer.Id);

      // load items
      ModelItems_CollectionChanged(BaseVM.Model.Items, null);
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, DU.Tree.OnItemsChanged onItemsChanged) {
      DU.Tree.SyncCollection<ViewerM, ViewerTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => DU.Tree.GetDestItem(model, model.Id, All, () => new(model, parent), null));
    }

    public override string GetTitle(ICatTreeViewItem item) => ((ViewerTreeVM)item).Model.Name;

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var viewerM = BaseVM.Model.ItemCreate(((ViewersTreeVM)root).BaseVM.Model, name);

      return All[viewerM.Id];
    }

    public override void ItemRename(ICatTreeViewItem item, string name) =>
      BaseVM.Model.ItemRename(((ViewerTreeVM)item).Model, name);

    public override string ValidateNewItemTitle(ICatTreeViewItem root, string name) =>
      BaseVM.Model.ItemCanRename(name) ? null : $"{name} item already exists!";

    public override void ItemDelete(ICatTreeViewItem item) =>
      BaseVM.Model.ItemDelete(((ViewerTreeVM)item).Model);
  }
}
