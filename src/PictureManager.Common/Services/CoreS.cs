using System.Linq;
using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Common.Models;
using PictureManager.Common.Repositories;

namespace PictureManager.Common.Services;

// TODO add Image inside MediaItem
public class CoreS(CoreR coreR) : ObservableObject {
  public FolderS Folder { get; } = new();
  public ImageS Image { get; } = new(coreR.Image);
  public MediaItemS MediaItem { get; } = new(coreR.MediaItem);
  public PersonS Person { get; } = new(coreR.Person);
  public SegmentS Segment { get; } = new(coreR.Segment);
  public ViewerS Viewer { get; } = new(coreR);

  public void AttachEvents() {
    coreR.Folder.ItemCreatedEvent += OnFolderCreated;
    coreR.Person.ItemDeletedEvent += OnPersonDeleted;
    coreR.Segment.ItemDeletedEvent += OnSegmentDeleted;
    coreR.Segment.SegmentsPersonChangedEvent += OnSegmentsPersonChanged;
  }

  private void OnFolderCreated(object? sender, FolderM item) {
    if (item is not DriveM drive || !coreR.Viewer.All.Any(x => x.IsDefault)) return;
    Log.Warning($"A new drive {drive.Name} was detected", "A new drive was detected and it won't be visible until it is added to the included folders of the current viewer or until the viewer is changed to All.");
  }

  private void OnPersonDeleted(object? sender, PersonM item) {
    Person.Selected.Set(item, false);
  }

  private void OnSegmentDeleted(object? sender, SegmentM item) {
    Segment.Selected.Set(item, false);
  }

  private void OnSegmentsPersonChanged(object? sender, (SegmentM[], PersonM?, PersonM[]) e) {
    Segment.Selected.DeselectAll();
  }
}