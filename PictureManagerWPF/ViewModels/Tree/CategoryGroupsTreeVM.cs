using System.Collections.Generic;
using System.Collections.ObjectModel;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Interfaces;
using DU = PictureManager.Domain.Utils;

namespace PictureManager.ViewModels.Tree {
  public sealed class CategoryGroupsTreeVM {
    public readonly Dictionary<int, CategoryGroupTreeVM> All = new();

    public void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ICatTreeViewItem> dest, ICatTreeViewItem parent, DU.Tree.OnItemsChangedCat onItemsChanged) {
      Domain.Utils.Tree.SyncCollection<CategoryGroupBaseVM, CategoryGroupTreeVM>(src, dest, parent,
        (baseVM, treeVM) => treeVM.BaseVM.Equals(baseVM),
        baseVM => DU.Tree.GetDestItemCat(baseVM, baseVM.Model.Id, All, () => new(baseVM, parent), onItemsChanged));
    }
  }
}
