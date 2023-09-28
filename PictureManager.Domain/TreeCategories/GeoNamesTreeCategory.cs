using MH.UI.BaseClasses;
using PictureManager.Domain.Database;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.TreeCategories;

public sealed class GeoNamesTreeCategory : TreeCategory<GeoNameM> {
  public GeoNamesTreeCategory(GeoNamesDataAdapter da) :
    base(Res.IconLocationCheckin, "GeoNames", (int)Category.GeoNames) {
    DataAdapter = da;
  }

  public override void OnItemSelected(object o) {
    if (o is GeoNameM g && Core.MediaItemsM.IsEditModeOn)
      Core.MediaItemsM.SetMetadata(g);
  }
}