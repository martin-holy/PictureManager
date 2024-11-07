using MH.UI.BaseClasses;

namespace PictureManager.Common.Features.GeoName;

public sealed class GeoNameTreeCategory(GeoNameR r)
  : TreeCategory<GeoNameM>(new(), Res.IconLocationCheckin, "GeoNames", (int)Category.GeoNames, r) {

  public override void OnItemSelected(object o) =>
    Core.VM.ToggleDialog.Toggle(o as GeoNameM);
}