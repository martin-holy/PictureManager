using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using DU = PictureManager.Domain.Utils;

namespace PictureManager.ViewModels.Tree {
  public sealed class PeopleTreeVM : BaseCatTreeViewCategory {
    private readonly AppCore _coreVM;

    public PeopleBaseVM BaseVM { get; }
    public readonly Dictionary<int, PersonTreeVM> All = new();

    public PeopleTreeVM(AppCore coreVM, PeopleBaseVM baseVM) : base(Category.People) {
      _coreVM = coreVM;
      BaseVM = baseVM;

      Title = "People";
      IconName = IconName.PeopleMultiple;
      CanHaveGroups = true;
      CanCreateItems = true;
      CanRenameItems = true;
      CanDeleteItems = true;
      CanMoveItem = true;

      BaseVM.Items.CollectionChanged += BaseVMItems_CollectionChanged;
      BaseVM.Model.PersonDeletedEvent += (_, e) => All.Remove(e.Person.Id);

      // load items
      BaseVMItems_CollectionChanged(BaseVM.Items, null);
    }

    private void BaseVMItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      // sync Category Groups
      _coreVM.CategoryGroupsTreeVM.SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
      // sync Items
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ICatTreeViewItem> dest, ICatTreeViewItem parent, DU.Tree.OnItemsChangedCat onItemsChanged) {
      Domain.Utils.Tree.SyncCollection<PersonBaseVM, PersonTreeVM>(src, dest, parent,
        (baseVM, treeVM) => treeVM.BaseVM.Equals(baseVM),
        baseVM => DU.Tree.GetDestItemCat(baseVM, baseVM.Model.Id, All, () => new(baseVM, parent), null));
    }

    public override string GetTitle(ICatTreeViewItem item) => ((PersonTreeVM)item).BaseVM.Model.Name;

    public override ICatTreeViewItem ItemCreate(ICatTreeViewItem root, string name) {
      var personM = BaseVM.Model.ItemCreate((ITreeBranch)ToModel(root), name);

      return All[personM.Id];
    }

    public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) =>
      BaseVM.Model.ItemMove(((PersonTreeVM)item).BaseVM.Model, (ITreeLeaf)ToModel(dest), aboveDest);

    public override void ItemRename(ICatTreeViewItem item, string name) =>
      BaseVM.Model.ItemRename(((PersonTreeVM)item).BaseVM.Model, name);

    public override string ValidateNewItemTitle(ICatTreeViewItem root, string name) =>
      BaseVM.Model.ItemCanRename(name) ? null : $"{name} item already exists!";

    public override string ValidateNewGroupTitle(ICatTreeViewItem root, string name) =>
      CategoryGroupsM.ItemCanRename((ITreeBranch)ToModel(root), name) ? null : $"{name} group already exists!";

    public override void ItemDelete(ICatTreeViewItem item) =>
      BaseVM.Model.ItemDelete(((PersonTreeVM)item).BaseVM.Model);

    private static object ToModel(ICatTreeViewItem item) =>
      item switch {
        PersonTreeVM x => x.BaseVM.Model,
        PeopleTreeVM x => x.BaseVM.Model,
        CategoryGroupTreeVM x => x.BaseVM.Model,
        _ => null
      };
  }
}
