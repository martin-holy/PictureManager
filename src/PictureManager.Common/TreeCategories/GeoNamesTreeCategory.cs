using MH.UI.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;

namespace PictureManager.Common.TreeCategories;

public sealed class GeoNamesTreeCategory(GeoNameR r)
  : TreeCategory<GeoNameM>(Res.IconLocationCheckin, "GeoNames", (int)Category.GeoNames, r) {

  public override void OnItemSelected(object o) =>
    Core.VM.ToggleDialog.Toggle(o as GeoNameM);
}