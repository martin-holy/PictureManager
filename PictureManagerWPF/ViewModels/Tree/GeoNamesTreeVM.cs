using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using MH.UI.WPF.BaseClasses;
using MH.Utils.Interfaces;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Properties;

namespace PictureManager.ViewModels.Tree {
  public sealed class GeoNamesTreeVM : CatTreeViewCategoryBase {
    public GeoNamesM Model { get; }
    public readonly Dictionary<int, GeoNameTreeVM> All = new();

    public static RelayCommand<GeoNamesTreeVM> NewGeoNameFromGpsCommand { get; } =
      new(NewGeoNameFromGps, cat => cat != null);

    public GeoNamesTreeVM(GeoNamesM model) : base(Category.GeoNames, "GeoNames") {
      Model = model;

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

    private static void NewGeoNameFromGps(GeoNamesTreeVM treeVM) {
      if (!GeoNamesBaseVM.IsGeoNamesUserNameInSettings()) return;

      var result = InputDialog.Open(
        "IconLocationCheckin",
        "GeoName latitude and longitude",
        "Enter in format: N36.75847,W3.84609",
        string.Empty,
        _ => null,
        out var output);

      if (!result) return;
      treeVM.Model.New(output, Settings.Default.GeoNamesUserName);
    }
  }
}
