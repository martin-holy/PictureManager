using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MH.UI.WPF.Interfaces;
using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class ViewersTreeVM : CatTreeViewCategoryBase {
    public ViewersBaseVM BaseVM { get; }
    public readonly Dictionary<int, ViewerTreeVM> All = new();

    public ViewersTreeVM(ViewersBaseVM baseVM) : base(Category.Viewers, "Viewers") {
      BaseVM = baseVM;

      BaseVM.Model.Items.CollectionChanged += ModelItems_CollectionChanged;
      BaseVM.Model.ViewerDeletedEvent += (_, e) => All.Remove(e.Viewer.Id);

      // load items
      ModelItems_CollectionChanged(BaseVM.Model.Items, null);
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
      All[BaseVM.Model.ItemCreate(root, name).Id];

    protected override void ModelItemRename(ICatTreeViewItem item, string name) =>
      BaseVM.Model.ItemRename(((ViewerTreeVM)item).Model, name);

    protected override void ModelItemDelete(ICatTreeViewItem item) =>
      BaseVM.Model.ItemDelete(((ViewerTreeVM)item).Model);

    protected override string ValidateNewItemName(ICatTreeViewItem root, string name) =>
      BaseVM.Model.ItemCanRename(name) ? null : $"{name} item already exists!";

    public override string GetTitle(object item) =>
      (item as ViewerTreeVM)?.Model.Name;
  }
}
