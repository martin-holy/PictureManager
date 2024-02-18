using MH.UI.BaseClasses;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Domain.Repositories;

namespace PictureManager.Domain.TreeCategories;

public sealed class GeoNamesTreeCategory : TreeCategory<GeoNameM> {
  public GeoNamesTreeCategory(GeoNameR r) :
    base(Res.IconLocationCheckin, "GeoNames", (int)Category.GeoNames) {
    DataAdapter = r;
  }

  public override void OnItemSelected(object o) =>
    ToggleDialogM.SetGeoName(o as GeoNameM);
}