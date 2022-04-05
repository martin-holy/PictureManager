using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MH.UI.WPF.Interfaces;
using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class ViewersTreeVM : CatTreeViewCategoryBase {
    public ViewersM Model { get; }
    public readonly Dictionary<int, ViewerTreeVM> All = new();

    public ViewersTreeVM(ViewersM model) : base(Category.Viewers, "Viewers") {
      Model = model;

      Model.Items.CollectionChanged += ModelItems_CollectionChanged;
      Model.ViewerDeletedEventHandler += (_, e) => All.Remove(e.Data.Id);

      // load items
      ModelItems_CollectionChanged(Model.Items, null);
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<ViewerM, ViewerTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => MH.Utils.Tree.GetDestItem(model, model.Id, All, () => new(model, parent), null));
    }

    protected override ICatTreeViewItem ModelItemCreate(ICatTreeViewItem root, string name) =>
      All[Model.ItemCreate(root, name).Id];

    protected override void ModelItemRename(ICatTreeViewItem item, string name) =>
      Model.ItemRename(((ViewerTreeVM)item).Model, name);

    protected override void ModelItemDelete(ICatTreeViewItem item) =>
      Model.ItemDelete(((ViewerTreeVM)item).Model);

    protected override string ValidateNewItemName(ICatTreeViewItem root, string name) =>
      Model.ItemCanRename(name) ? null : $"{name} item already exists!";

    public override string GetTitle(object item) =>
      (item as ViewerTreeVM)?.Model.Name;
  }
}
