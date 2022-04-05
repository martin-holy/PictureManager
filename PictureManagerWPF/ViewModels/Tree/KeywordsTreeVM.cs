using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MH.UI.WPF.Interfaces;
using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class KeywordsTreeVM : CatTreeViewCategoryBase {
    private readonly CategoryGroupsTreeVM _categoryGroupsTreeVM;

    public KeywordsM KeywordsM { get; }
    public readonly Dictionary<int, KeywordTreeVM> All = new();

    public KeywordsTreeVM(KeywordsM keywordsM, CategoryGroupsTreeVM categoryGroupsTreeVM) : base(Category.Keywords, "Keywords") {
      _categoryGroupsTreeVM = categoryGroupsTreeVM;
      KeywordsM = keywordsM;
      CanMoveItem = true;

      KeywordsM.Items.CollectionChanged += ModelItems_CollectionChanged;
      KeywordsM.KeywordDeletedEventHandler += (_, e) => All.Remove(e.Data.Id);

      // load items
      ModelItems_CollectionChanged(KeywordsM.Items, null);
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      // sync Category Groups
      _categoryGroupsTreeVM.SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
      // sync Items
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<KeywordM, KeywordTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => MH.Utils.Tree.GetDestItem(model, model.Id, All, () => new(model, parent), onItemsChanged));
    }

    protected override ICatTreeViewItem ModelItemCreate(ICatTreeViewItem root, string name) =>
      All[KeywordsM.ItemCreate((ITreeBranch)ToModel(root), name).Id];

    protected override void ModelItemRename(ICatTreeViewItem item, string name) =>
      KeywordsM.ItemRename((KeywordM)ToModel(item), name);

    protected override void ModelItemDelete(ICatTreeViewItem item) =>
      KeywordsM.ItemDelete((KeywordM)ToModel(item));

    public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) =>
      KeywordsM.ItemMove((KeywordM)ToModel(item), (ITreeLeaf)ToModel(dest), aboveDest);

    protected override string ValidateNewItemName(ICatTreeViewItem root, string name) =>
      KeywordsM.ItemCanRename((ITreeBranch)ToModel(root), name) ? null : $"{name} item already exists!";

    protected override void ModelGroupCreate(ICatTreeViewItem root, string name) =>
      _categoryGroupsTreeVM.CategoryGroupsM.GroupCreate(name, Category);

    protected override void ModelGroupRename(ICatTreeViewGroup group, string name) =>
      _categoryGroupsTreeVM.CategoryGroupsM.GroupRename((CategoryGroupM)ToModel(group), name);

    protected override void ModelGroupDelete(ICatTreeViewGroup group) =>
      _categoryGroupsTreeVM.CategoryGroupsM.GroupDelete((CategoryGroupM)ToModel(group));

    public override void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) =>
      _categoryGroupsTreeVM.CategoryGroupsM.GroupMove((CategoryGroupM)ToModel(group), (CategoryGroupM)ToModel(dest), aboveDest);

    protected override string ValidateNewGroupName(ICatTreeViewItem root, string name) =>
      CategoryGroupsM.ItemCanRename((ITreeBranch)ToModel(root), name) ? null : $"{name} group already exists!";

    public override string GetTitle(object item) =>
      ToModel(item) switch {
        KeywordM x => x.Name,
        CategoryGroupM x => x.Name,
        _ => null
      };

    private static object ToModel(object item) =>
      item switch {
        KeywordTreeVM x => x.Model,
        KeywordsTreeVM x => x.KeywordsM,
        CategoryGroupTreeVM x => x.Model,
        _ => null
      };
  }
}
