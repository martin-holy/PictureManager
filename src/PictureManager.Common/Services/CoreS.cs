using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;
using PictureManager.Interfaces.Services;

namespace PictureManager.Common.Services;

// TODO add Image inside MediaItem
public class CoreS(CoreR coreR) : ObservableObject, ICoreS {
  public FolderS Folder { get; } = new();
  public ImageS Image { get; } = new(coreR.Image);
  public MediaItemS MediaItem { get; } = new(coreR.MediaItem);
  public PersonS Person { get; } = new(coreR.Person);
  public SegmentS Segment { get; } = new(coreR.Segment);
  public ViewerS Viewer { get; } = new(coreR);

  IMediaItemS ICoreS.MediaItem => MediaItem;
  ISegmentS ICoreS.Segment => Segment;

  public void AttachEvents() {
    coreR.Person.ItemDeletedEvent += OnPersonDeleted;
    coreR.Segment.ItemDeletedEvent += OnSegmentDeleted;
    coreR.Segment.SegmentsPersonChangedEvent += OnSegmentsPersonChanged;
  }

  private void OnPersonDeleted(object sender, PersonM item) {
    Person.Selected.Set(item, false);
  }

  private void OnSegmentDeleted(object sender, SegmentM item) {
    Segment.Selected.Set(item, false);
  }

  private void OnSegmentsPersonChanged(object sender, (SegmentM[], PersonM, PersonM[]) e) {
    Segment.Selected.DeselectAll();
  }
}