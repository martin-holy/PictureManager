using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Common.Features.Common;
using PictureManager.Common.Features.MediaItem;
using PictureManager.Common.Features.Person;
using System.Collections.Generic;
using System.Linq;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Common.Features.Segment;

public sealed class SegmentsMatchingVM : ObservableObject {
  public CanDragFunc CanDragFunc { get; }
  public PersonCollectionView CvPeople { get; } = new();
  public SegmentCollectionView CvSegments { get; } = new();

  public SegmentsMatchingVM(SegmentS segmentS) {
    CanDragFunc = one => segmentS.GetOneOrSelected(one as SegmentM);
  }

  public static int GetSegmentsToLoadUserInput() {
    var md = new MessageDialog("Segments", "Load segments from ...", Res.IconSegment, true);

    md.Buttons = [
      new(md.SetResult(1, MH.UI.Res.IconImage, "Media items"), true),
      new(md.SetResult(2, Res.IconPeople, "People")),
      new(md.SetResult(3, Res.IconSegment, "Segments"))
    ];

    return Dialog.Show(md);
  }

  public static IEnumerable<SegmentM> GetSegments(int mode) {
    switch (mode) {
      case 1:
        var items = Core.VM.MediaViewer.IsVisible
          ? Core.VM.MediaViewer.Current != null ? [Core.VM.MediaViewer.Current] : []
          : Core.VM.MediaItem.Views.Current?.GetSelectedOrAll().ToArray() ?? [];

        return items.Concat(items.GetVideoItems()).GetSegments();
      case 2:
        var people = Core.S.Person.Selected.Items.ToHashSet();

        return Core.R.Segment.All
          .Where(x => x.Person != null && people.Contains(x.Person))
          .OrderBy(x => x.MediaItem.FileName);
      case 3:
        return Core.S.Segment.Selected.Items;
      default:
        return [];
    }
  }

  public void OnSegmentsPersonChanged(SegmentM[] segments) {
    CvSegments.Insert(segments);
    var newP = CvSegments.Root?.Source.GetPeople().ToArray()!;
    var oldP = CvPeople.Root?.Source.EmptyIfNull().ToArray()!;
    var pIn = newP.Except(oldP).ToArray();
    var pOut = oldP.Except(newP).ToArray();
    CvPeople.Insert(pIn);
    CvPeople.Remove(pOut);
  }

  public void Reload(SegmentM[] items) {
    ReloadPeople(items);
    ReloadSegments(items);
  }

  private void ReloadPeople(SegmentM[] items) {
    var source = items.GetPeople().OrderBy(x => x.Name).ToList();
    CvPeople.Reload(source, GroupMode.GroupByRecursive, null, true);
  }

  private void ReloadSegments(SegmentM[] items) {
    var source = items.OrderBy(x => x.MediaItem.FileName).ToList();
    var groupByItems = new[] {
      GroupByItems.GetPeopleInGroup(items),
      GroupByItems.GetKeywordsInGroup(items)
    };

    CvSegments.Reload(source, GroupMode.ThenByRecursive, groupByItems, true);
  }
}