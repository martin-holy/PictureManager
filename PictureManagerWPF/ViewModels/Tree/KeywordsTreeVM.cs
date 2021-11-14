using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MH.UI.WPF.Interfaces;
using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class KeywordsTreeVM : CatTreeViewCategoryBase {
    private readonly Core _core;
    private readonly AppCore _coreVM;

    public KeywordsBaseVM BaseVM { get; }
    public readonly Dictionary<int, KeywordTreeVM> All = new();

    public KeywordsTreeVM(Core core, AppCore coreVM, KeywordsBaseVM baseVM) : base(Category.Keywords, "Keywords") {
      _core = core;
      _coreVM = coreVM;
      BaseVM = baseVM;
      CanMoveItem = true;

      BaseVM.Items.CollectionChanged += BaseVMItems_CollectionChanged;
      BaseVM.Model.KeywordDeletedEvent += (_, e) => All.Remove(e.Keyword.Id);

      // load items
      BaseVMItems_CollectionChanged(BaseVM.Items, null);
    }

    private void BaseVMItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      // sync Category Groups
      _coreVM.CategoryGroupsTreeVM.SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
      // sync Items
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<KeywordBaseVM, KeywordTreeVM>(src, dest, parent,
        (baseVM, treeVM) => treeVM.BaseVM.Equals(baseVM),
        baseVM => MH.Utils.Tree.GetDestItem(baseVM, baseVM.Model.Id, All, () => new(baseVM, parent), onItemsChanged));
    }

    protected override ICatTreeViewItem ModelItemCreate(ICatTreeViewItem root, string name) =>
      All[BaseVM.Model.ItemCreate((ITreeBranch)ToModel(root), name).Id];

    protected override void ModelItemRename(ICatTreeViewItem item, string name) =>
      BaseVM.Model.ItemRename((KeywordM)ToModel(item), name);

    protected override void ModelItemDelete(ICatTreeViewItem item) =>
      BaseVM.Model.ItemDelete((KeywordM)ToModel(item));

    public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) =>
      BaseVM.Model.ItemMove((KeywordM)ToModel(item), (ITreeLeaf)ToModel(dest), aboveDest);

    protected override string ValidateNewItemName(ICatTreeViewItem root, string name) =>
      KeywordsM.ItemCanRename((ITreeBranch)ToModel(root), name) ? null : $"{name} item already exists!";

    protected override void ModelGroupCreate(ICatTreeViewItem root, string name) =>
      _core.CategoryGroupsM.GroupCreate(name, Category);

    protected override void ModelGroupRename(ICatTreeViewGroup group, string name) =>
      _core.CategoryGroupsM.GroupRename((CategoryGroupM)ToModel(group), name);

    protected override void ModelGroupDelete(ICatTreeViewGroup group) =>
      _core.CategoryGroupsM.GroupDelete((CategoryGroupM)ToModel(group));

    public override void GroupMove(ICatTreeViewGroup group, ICatTreeViewGroup dest, bool aboveDest) =>
      _core.CategoryGroupsM.GroupMove((CategoryGroupM)ToModel(group), (CategoryGroupM)ToModel(dest), aboveDest);

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
        KeywordTreeVM x => x.BaseVM.Model,
        KeywordsTreeVM x => x.BaseVM.Model,
        CategoryGroupTreeVM x => x.BaseVM.Model,
        _ => null
      };
  }
}
