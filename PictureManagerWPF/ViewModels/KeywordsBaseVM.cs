using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using DU = PictureManager.Domain.Utils;

namespace PictureManager.ViewModels {
  public class KeywordsBaseVM : ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    private readonly AppCore _coreVM;

    public KeywordsM Model { get; }
    public Dictionary<int, KeywordBaseVM> All { get; } = new();

    public KeywordsBaseVM(AppCore coreVM, KeywordsM model) {
      _coreVM = coreVM;
      Model = model;

      Model.Items.CollectionChanged += ModelItems_CollectionChanged;
      Model.KeywordDeletedEvent += (_, e) => All.Remove(e.Keyword.Id);

      // load items
      ModelItems_CollectionChanged(Model.Items, null);
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      // sync Category Groups
      _coreVM.CategoryGroupsBaseVM.SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
      // sync Items
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);
    }

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, DU.Tree.OnItemsChanged onItemsChanged) {
      Domain.Utils.Tree.SyncCollection<KeywordM, KeywordBaseVM>(src, dest, parent,
        (m, vm) => vm.Model.Equals(m),
        m => DU.Tree.GetDestItem(m, m.Id, All, () => new(m, parent), onItemsChanged));
    }
  }
}
