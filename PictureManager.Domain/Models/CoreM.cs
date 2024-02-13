using PictureManager.Domain.Models.MediaItems;

namespace PictureManager.Domain.Models;

public class CoreM {
  public GeoNamesM GeoNames { get; }
  public MediaItemsM MediaItems { get; }
  public MediaItemsStatusBarM MediaItemsStatusBar { get; }

  public CoreM() {
    GeoNames = Core.GeoNamesM;
    MediaItems = Core.Db.MediaItems.Model;
    MediaItemsStatusBar = Core.MediaItemsStatusBarM;
  }
}