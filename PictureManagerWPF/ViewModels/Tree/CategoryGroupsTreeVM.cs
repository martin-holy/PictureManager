using System.Collections.Generic;
using System.Collections.ObjectModel;
using MH.Utils.Interfaces;

namespace PictureManager.ViewModels.Tree {
  public sealed class CategoryGroupsTreeVM {
    public readonly Dictionary<int, CategoryGroupTreeVM> All = new();

    public void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<CategoryGroupBaseVM, CategoryGroupTreeVM>(src, dest, parent,
        (baseVM, treeVM) => treeVM.BaseVM.Equals(baseVM),
        baseVM => MH.Utils.Tree.GetDestItem(baseVM, baseVM.Model.Id, All, () => new(baseVM, parent), onItemsChanged));
    }
  }
}
