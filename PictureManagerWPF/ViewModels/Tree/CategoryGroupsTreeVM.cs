using System.Collections.Generic;
using System.Collections.ObjectModel;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class CategoryGroupsTreeVM {
    public readonly Dictionary<int, CategoryGroupTreeVM> All = new();

    public void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<CategoryGroupM, CategoryGroupTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => MH.Utils.Tree.GetDestItem(model, model.Id, All, () => new(model, parent), onItemsChanged));
    }
  }
}
