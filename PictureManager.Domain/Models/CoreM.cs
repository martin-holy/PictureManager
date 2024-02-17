using PictureManager.Domain.Database;
using PictureManager.Domain.Models.MediaItems;

namespace PictureManager.Domain.Models;

public class CoreM {
  public GeoNamesM GeoNames { get; }
  public ImagesM Images { get; }
  public MediaItemsM MediaItems { get; }
  public MediaItemsStatusBarM MediaItemsStatusBar { get; }

  public CoreM(Db db) {
    GeoNames = Core.GeoNamesM;
    Images = new(db.Images);
    MediaItems = new(db.MediaItems);
    MediaItemsStatusBar = Core.MediaItemsStatusBarM;
  }
}