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

  public CoreS(CoreR r) {
    Folder = new(r.Folder);
    Image = new(r.Image);
    MediaItem = new(r.MediaItem);
    MediaItemsStatusBar = Core.MediaItemsStatusBarM;
    Person = new(r.Person);
    Segment = r.Segment.Service;
    Viewer = new(r.Viewer);
  }
}