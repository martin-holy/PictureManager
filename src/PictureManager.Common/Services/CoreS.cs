using MH.Utils.BaseClasses;
using PictureManager.Common.Repositories;

namespace PictureManager.Common.Services;

// TODO add Image inside MediaItem
public class CoreS(CoreR coreR) : ObservableObject {
  public FolderS Folder { get; } = new();
  public ImageS Image { get; } = new(coreR.Image);
  public MediaItemS MediaItem { get; } = new(coreR.MediaItem);
  public PersonS Person { get; } = new(coreR.Person);
  public SegmentS Segment { get; } = new(coreR.Segment);
  public ViewerS Viewer { get; } = new(coreR.Viewer);
}