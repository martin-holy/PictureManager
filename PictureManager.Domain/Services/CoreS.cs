using PictureManager.Domain.Repositories;

namespace PictureManager.Domain.Services;

public class CoreS(CoreR coreR) {
  public FolderS Folder { get; } = new(coreR.Folder);
  public ImageS Image { get; } = new(coreR.Image);
  public MediaItemS MediaItem { get; } = new(coreR.MediaItem);
  public PersonS Person { get; } = new(coreR.Person);
  public SegmentS Segment { get; } = new(coreR.Segment);
  public ViewerS Viewer { get; } = new(coreR.Viewer);
}