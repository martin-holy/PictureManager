using MH.UI.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories;

public sealed class GeoNamesTreeCategory : TreeCategory<GeoNameM> {
  public GeoNamesTreeCategory(GeoNamesDA da) :
    base(Res.IconLocationCheckin, "GeoNames", (int)Category.GeoNames) {
    DataAdapter = da;
  }

  public override void OnItemSelected(object o) =>
    ToggleDialogM.SetGeoName(o as GeoNameM);
}