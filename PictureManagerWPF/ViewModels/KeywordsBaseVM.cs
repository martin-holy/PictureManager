using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

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

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) {
      MH.Utils.Tree.SyncCollection<KeywordM, KeywordBaseVM>(src, dest, parent,
        (m, vm) => vm.Model.Equals(m),
        m => MH.Utils.Tree.GetDestItem(m, m.Id, All, () => new(m, parent), onItemsChanged));
    }
  }
}
