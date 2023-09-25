using MH.UI.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories;

public sealed class GeoNamesTreeCategory : TreeCategory<GeoNameM> {
  public GeoNamesTreeCategory() : base(Res.IconLocationCheckin, "GeoNames", (int)Category.GeoNames) {
    DataAdapter = Core.Db.GeoNames = new(this);
  }

  public override void OnItemSelected(object o) {
    if (o is GeoNameM g && Core.MediaItemsM.IsEditModeOn)
      Core.MediaItemsM.SetMetadata(g);
  }
}