using MH.UI.BaseClasses;
using PictureManager.Common.Dialogs;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;

namespace PictureManager.Common.TreeCategories;

public sealed class GeoNamesTreeCategory : TreeCategory<GeoNameM> {
  public GeoNamesTreeCategory(GeoNameR r) :
    base(Res.IconLocationCheckin, "GeoNames", (int)Category.GeoNames) {
    DataAdapter = r;
  }

  public override void OnItemSelected(object o) =>
    ToggleDialogM.SetGeoName(o as GeoNameM);
}