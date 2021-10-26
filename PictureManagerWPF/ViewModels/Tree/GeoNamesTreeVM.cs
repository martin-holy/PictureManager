using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PictureManager.Domain;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels.Tree {
  public sealed class GeoNamesTreeVM : BaseCatTreeViewCategory {
    public GeoNamesM Model { get; }
    public readonly Dictionary<int, GeoNameTreeVM> All = new();

    public GeoNamesTreeVM(GeoNamesM model) : base(Category.GeoNames) {
      Model = model;

      Title = "GeoNames";
      IconName = IconName.LocationCheckin;

      Model.Items.CollectionChanged += ModelItems_CollectionChanged;

      // load items
      ModelItems_CollectionChanged(Model.Items, null);
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, Domain.Utils.Tree.OnItemsChanged onItemsChanged) =>
      Domain.Utils.Tree.SyncCollection<GeoNameM, GeoNameTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => Domain.Utils.Tree.GetDestItem(model, model.Id, All, () => new(model, parent), onItemsChanged));
  }
}
