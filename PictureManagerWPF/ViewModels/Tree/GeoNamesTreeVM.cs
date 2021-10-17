using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
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
      SyncCollection((ObservableCollection<object>)sender, Items, this, SyncCollection);

    private void SyncCollection(ObservableCollection<object> src, ObservableCollection<ICatTreeViewItem> dest, ICatTreeViewItem parent, Domain.Utils.Tree.OnItemsChangedCat onItemsChanged) =>
      Domain.Utils.Tree.SyncCollection<GeoNameM, GeoNameTreeVM>(src, dest, parent,
        (model, treeVM) => treeVM.Model.Equals(model),
        model => Domain.Utils.Tree.GetDestItemCat(model, model.Id, All, () => new(model, parent), onItemsChanged));
  }
}
