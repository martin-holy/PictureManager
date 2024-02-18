using PictureManager.Domain.Models.MediaItems;
using PictureManager.Domain.Repositories;

namespace PictureManager.Domain.Services;

public class CoreS {
  public FolderS Folder { get; }
  public ImageS Image { get; }
  public MediaItemS MediaItem { get; }
  public MediaItemsStatusBarM MediaItemsStatusBar { get; }
  public PersonS Person { get; }
  public SegmentS Segment { get; }
  public ViewerS Viewer { get; }

  public CoreS(CoreR coreR) {
    Folder = new(coreR.Folder);
    Image = new(coreR.Image);
    MediaItem = new(coreR.MediaItem);
    MediaItemsStatusBar = Core.MediaItemsStatusBarM;
    Person = new(coreR.Person);
    Segment = new(coreR.Segment);
    Viewer = new(coreR.Viewer);
  }
}