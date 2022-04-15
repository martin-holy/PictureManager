using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MH.UI.WPF.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Properties;

namespace PictureManager.ViewModels.Tree {
  public sealed class GeoNamesTreeVM : CatTreeViewCategoryBase {
    public GeoNamesM Model { get; }
    public readonly Dictionary<int, GeoNameTreeVM> All = new();
    public RelayCommand<GeoNamesTreeVM> NewGeoNameFromGpsCommand { get; }

    public GeoNamesTreeVM(GeoNamesM model) : base(Category.GeoNames, "GeoNames") {
      Model = model;
      NewGeoNameFromGpsCommand = new(() => Model.NewGeoNameFromGps(Settings.Default.GeoNamesUserName));
      Model.Items.CollectionChanged += ModelItems_CollectionChanged;

      // load items
      ModelItems_CollectionChanged(Model.Items, null);
    }

    private void ModelItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) =>
      SyncCollection((ObservableCollection<ITreeLeaf>)sender, Items, this, SyncCollection);

    private void SyncCollection(ObservableCollection<ITreeLeaf> src, ObservableCollection<ITreeLeaf> dest, ITreeBranch parent, MH.Utils.Tree.OnItemsChanged onItemsChanged) =>
      MH.Utils.Tree.SyncCollection<GeoNameM, GeoNameTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => MH.Utils.Tree.GetDestItem(model, model.Id, All, () => new(model, parent), onItemsChanged));
  }
}
