using System.Collections.Generic;
using System.Collections.ObjectModel;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using DU = PictureManager.Domain.Utils;

namespace PictureManager.ViewModels {
  public sealed class CategoryGroupsBaseVM {
    public readonly Dictionary<int, CategoryGroupBaseVM> All = new();

    public void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, DU.Tree.OnItemsChanged onItemsChanged) {
      Domain.Utils.Tree.SyncCollection<CategoryGroupM, CategoryGroupBaseVM>(src, dest, parent,
        (m, vm) => vm.Model.Equals(m),
        m => DU.Tree.GetDestItem(m, m.Id, All, () => new(m, parent), onItemsChanged));
    }
  }
}
