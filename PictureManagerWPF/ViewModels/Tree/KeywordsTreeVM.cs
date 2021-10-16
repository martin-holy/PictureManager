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
      _coreVM.CategoryGroupsTreeVM.SyncCollection((ObservableCollection<object>)sender, Items, this, SyncCollection);
      // sync Items
      SyncCollection((ObservableCollection<object>)sender, Items, this, SyncCollection);
    }

    private void SyncCollection(ObservableCollection<object> src, ObservableCollection<ICatTreeViewItem> dest, ICatTreeViewItem parent, DU.Tree.OnItemsChangedCat onItemsChanged) {
      Domain.Utils.Tree.SyncCollection<KeywordBaseVM, KeywordTreeVM>(src, dest, parent,
        (baseVM, treeVM) => treeVM.BaseVM.Equals(baseVM),
        baseVM => DU.Tree.GetDestItemCat(baseVM, baseVM.Model.Id, All, () => new(baseVM, parent), onItemsChanged));
    }

    public override string GetTitle(ICatTreeViewItem item) => ((KeywordTreeVM)item).BaseVM.Model.Name;

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var keywordM = BaseVM.Model.ItemCreate(ToModel(root), name);

      return All[keywordM.Id];
    }

    public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) =>
      BaseVM.Model.ItemMove(((KeywordTreeVM)item).BaseVM.Model, ToModel(dest), aboveDest);

    public override void ItemRename(ICatTreeViewItem item, string name) =>
      BaseVM.Model.ItemRename(((KeywordTreeVM)item).BaseVM.Model, name);

    public override string ValidateNewItemTitle(ICatTreeViewItem root, string name) =>
      KeywordsM.ItemCanRename(ToModel(root), name) ? null : $"{name} item already exists!";

    public override string ValidateNewGroupTitle(ICatTreeViewItem root, string name) =>
      CategoryGroupsM.ItemCanRename(ToModel(root), name) ? null : $"{name} group already exists!";

    public override void ItemDelete(ICatTreeViewItem item) {
      if (item is not KeywordTreeVM keywordTreeVM) return;
      
      BaseVM.Model.ItemDelete(keywordTreeVM.BaseVM.Model);
    }

    private static ITreeBranch ToModel(ICatTreeViewItem item) =>
      item switch {
        KeywordTreeVM x => x.BaseVM.Model,
        KeywordsTreeVM x => x.BaseVM.Model,
        CategoryGroupTreeVM x => x.BaseVM.Model,
        _ => null
      };
  }
}
