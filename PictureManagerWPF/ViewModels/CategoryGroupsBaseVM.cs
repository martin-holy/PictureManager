using System.Collections.Generic;
using System.Collections.ObjectModel;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class CategoryGroupsBaseVM {
    public readonly Dictionary<int, CategoryGroupBaseVM> All = new();

    public void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<CategoryGroupM, CategoryGroupBaseVM>(src, dest, parent,
        (m, vm) => vm.Model.Equals(m),
        m => MH.Utils.Tree.GetDestItem(m, m.Id, All, () => new(m, parent), onItemsChanged));
    }
  }
}
