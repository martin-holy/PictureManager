﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MH.UI.WPF.Interfaces;
using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class PeopleTreeVM : CatTreeViewCategoryBase {
    private readonly Core _core;
    private readonly AppCore _coreVM;

    public PeopleM Model { get; }
    public readonly Dictionary<int, PersonTreeVM> All = new();

    public PeopleTreeVM(Core core, AppCore coreVM, PeopleM model) : base(Category.People, "People") {
      _core = core;
      _coreVM = coreVM;
      Model = model;
      CanMoveItem = true;

      Model.Items.CollectionChanged += ModelItems_CollectionChanged;
      Model.PersonDeletedEvent += (_, e) => All.Remove(e.Person.Id);

      // load items
      ModelItems_CollectionChanged(Model.Items, null);
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      // sync Category Groups
      _coreVM.CategoryGroupsTreeVM.SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
      // sync Items
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<PersonM, PersonTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => MH.Utils.Tree.GetDestItem(model, model.Id, All, () => new(model, parent), null));
    }

    protected override ICatTreeViewItem ModelItemCreate(ICatTreeViewItem root, string name) =>
      All[Model.ItemCreate((ITreeBranch)ToModel(root), name).Id];

    protected override void ModelItemRename(ICatTreeViewItem item, string name) =>
      Model.ItemRename((PersonM)ToModel(item), name);

    protected override void ModelItemDelete(ICatTreeViewItem item) =>
      Model.ItemDelete((PersonM)ToModel(item));

    public override void ItemMove(ICatTreeViewItem item, ICatTreeViewItem dest, bool aboveDest) =>
      Model.ItemMove((PersonM)ToModel(item), (ITreeLeaf)ToModel(dest), aboveDest);

    protected override string ValidateNewItemName(ICatTreeViewItem root, string name) =>
      Model.ItemCanRename(name) ? null : $"{name} item already exists!";

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
        PersonM x => x.Name,
        CategoryGroupM x => x.Name,
        _ => null
      };

    private static object ToModel(object item) =>
      item switch {
        PersonTreeVM x => x.Model,
        PeopleTreeVM x => x.Model,
        CategoryGroupTreeVM x => x.Model,
        _ => null
      };
  }
}
