using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using DU = PictureManager.Domain.Utils;

namespace PictureManager.ViewModels.Tree {
  public sealed class KeywordsTreeVM : BaseCatTreeViewCategory {
    private readonly AppCore _coreVM;

    public KeywordsBaseVM BaseVM { get; }
    public readonly Dictionary<int, KeywordTreeVM> All = new();

    public KeywordsTreeVM(AppCore coreVM, KeywordsBaseVM baseVM) : base(Category.Keywords) {
      _coreVM = coreVM;
      BaseVM = baseVM;

      Title = "Keywords";
      IconName = IconName.TagLabel;
      CanHaveGroups = true;
      CanHaveSubItems = true;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;
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

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, DU.Tree.OnItemsChanged onItemsChanged) {
      Domain.Utils.Tree.SyncCollection<KeywordBaseVM, KeywordTreeVM>(src, dest, parent,
        (baseVM, treeVM) => treeVM.BaseVM.Equals(baseVM),
        baseVM => DU.Tree.GetDestItem(baseVM, baseVM.Model.Id, All, () => new(baseVM, parent), onItemsChanged));
    }

    public override string GetTitle(ICatTreeViewItem item) => TreeToModel(item).Name;

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var keywordM = BaseVM.Model.ItemCreate(ToTreeBranch(root), name);

      return All[keywordM.Id];
    }

    public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) =>
      BaseVM.Model.ItemMove(TreeToModel(item), ToTreeBranch(dest), aboveDest);

    public override void ItemRename(ICatTreeViewItem item, string name) =>
      BaseVM.Model.ItemRename(TreeToModel(item), name);

    public override string ValidateNewItemTitle(ICatTreeViewItem root, string name) =>
      KeywordsM.ItemCanRename(ToTreeBranch(root), name) ? null : $"{name} item already exists!";

    public override string ValidateNewGroupTitle(ICatTreeViewItem root, string name) =>
      CategoryGroupsM.ItemCanRename(ToTreeBranch(root), name) ? null : $"{name} group already exists!";

    public override void ItemDelete(ICatTreeViewItem item) =>
      BaseVM.Model.ItemDelete(TreeToModel(item));

    private static KeywordM TreeToModel(object item) => ((KeywordTreeVM)item).BaseVM.Model;

    private static ITreeBranch ToTreeBranch(object item) =>
      item switch {
        KeywordTreeVM x => x.BaseVM.Model,
        KeywordsTreeVM x => x.BaseVM.Model,
        CategoryGroupTreeVM x => x.BaseVM.Model,
        _ => null
      };
  }
}
