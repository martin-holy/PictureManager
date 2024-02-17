using PictureManager.Domain.Database;
using PictureManager.Domain.Models.MediaItems;

namespace PictureManager.Domain.Models;

public class CoreM {
  public ImagesM Images { get; }
  public MediaItemsM MediaItems { get; }
  public MediaItemsStatusBarM MediaItemsStatusBar { get; }
  public SegmentsM Segments { get; }

  public CoreM(Db db) {
    Images = new(db.Images);
    MediaItems = new(db.MediaItems);
    MediaItemsStatusBar = Core.MediaItemsStatusBarM;
    Segments = db.Segments.Model;
  }
}