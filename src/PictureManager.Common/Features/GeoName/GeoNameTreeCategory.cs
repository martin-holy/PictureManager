using MH.UI.BaseClasses;

namespace PictureManager.Common.Features.GeoName;

public sealed class GeoNameTreeCategory(GeoNameR r)
  : TreeCategory<GeoNameM>(new(), Res.IconLocationCheckin, "GeoNames", (int)Category.GeoNames, r) {

  protected override void _onItemSelected(object o) =>
    _ = Core.VM.ToggleDialog.Toggle(o as GeoNameM);
}